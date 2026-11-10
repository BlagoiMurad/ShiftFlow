using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Data.Entities;
using ShiftFlow.Data.Enums;
using ShiftFlow.Services.Common;

namespace ShiftFlow.Web.Data;

/// <summary>
/// Прилага миграциите и създава ролите. Демонстрационните данни (с известни пароли)
/// се добавят само в среда Development. В друга среда се създава само администратор
/// с имейл и парола от настройките Seed:AdminEmail и Seed:AdminPassword.
/// </summary>
public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services, bool includeDemoData, IConfiguration configuration)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        var users = scope.ServiceProvider.GetRequiredService<UserManager<Employee>>();
        var roles = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();

        await db.Database.MigrateAsync();

        foreach (var role in Roles.All)
            if (!await roles.RoleExistsAsync(role))
                await roles.CreateAsync(new IdentityRole(role));

        if (await db.Users.AnyAsync()) return;

        if (!includeDemoData)
        {
            var adminEmail = configuration["Seed:AdminEmail"];
            var adminPassword = configuration["Seed:AdminPassword"];
            if (string.IsNullOrWhiteSpace(adminEmail) || string.IsNullOrWhiteSpace(adminPassword))
                return;
            var admin = new Employee
            {
                UserName = adminEmail, Email = adminEmail, EmailConfirmed = true,
                FirstName = "Системен", LastName = "Администратор"
            };
            var created = await users.CreateAsync(admin, adminPassword);
            if (!created.Succeeded)
                throw new InvalidOperationException(string.Join(" ", created.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(admin, Roles.Admin);
            return;
        }

        var store = new Department { Name = "Магазин Център", Description = "Търговска зала и каси", MinStaffPerShift = 2 };
        var warehouse = new Department { Name = "Склад", Description = "Приемане и подреждане на стока", MinStaffPerShift = 1 };
        db.Departments.AddRange(store, warehouse);

        var cashier = new Position { Name = "Касиер", HourlyRate = 9.50m };
        var stocker = new Position { Name = "Складов работник", HourlyRate = 10.00m };
        var managerPos = new Position { Name = "Управител", HourlyRate = 15.00m };
        db.Positions.AddRange(cashier, stocker, managerPos);
        await db.SaveChangesAsync();

        async Task<Employee> AddUser(string email, string first, string last, string password, string role, Department? dep, Position? pos)
        {
            var user = new Employee
            {
                UserName = email, Email = email, EmailConfirmed = true,
                FirstName = first, LastName = last,
                DepartmentId = dep?.Id, PositionId = pos?.Id,
                HireDate = DateTime.Today.AddYears(-1)
            };
            var result = await users.CreateAsync(user, password);
            if (!result.Succeeded)
                throw new InvalidOperationException(string.Join(" ", result.Errors.Select(e => e.Description)));
            await users.AddToRoleAsync(user, role);
            return user;
        }

        await AddUser("admin@shiftflow.bg", "Системен", "Администратор", "Admin123", Roles.Admin, null, null);
        var manager = await AddUser("manager@shiftflow.bg", "Петя", "Иванова", "Manager123", Roles.Manager, store, managerPos);
        var ivan = await AddUser("ivan@shiftflow.bg", "Иван", "Петров", "Employee123", Roles.Employee, store, cashier);
        var maria = await AddUser("maria@shiftflow.bg", "Мария", "Георгиева", "Employee123", Roles.Employee, store, cashier);
        var georgi = await AddUser("georgi@shiftflow.bg", "Георги", "Димитров", "Employee123", Roles.Employee, warehouse, stocker);
        var elena = await AddUser("elena@shiftflow.bg", "Елена", "Стоянова", "Employee123", Roles.Employee, warehouse, stocker);

        // Смени за текущата и следващата седмица (пон–пет): сутрешна 07–15 и следобедна 15–23.
        // Редуват се по седмици, а не по дни, за да има поне 11 часа почивка между смените.
        var monday = WeekStart(DateTime.Today);
        var pairs = new[] { (ivan, maria, store), (georgi, elena, warehouse) };
        for (int day = 0; day < 12; day++)
        {
            var date = monday.AddDays(day);
            if (date.DayOfWeek is DayOfWeek.Saturday or DayOfWeek.Sunday) continue;
            foreach (var (a, b, dep) in pairs)
            {
                var (morning, evening) = (day / 7) % 2 == 0 ? (a, b) : (b, a);
                db.Shifts.Add(NewShift(morning, dep, date.AddHours(7), date.AddHours(15)));
                db.Shifts.Add(NewShift(evening, dep, date.AddHours(15), date.AddHours(23)));
            }
        }
        await db.SaveChangesAsync();

        // Отчетено време за изминалите смени (с малки закъснения за пример).
        var rnd = new Random(42);
        var pastShifts = await db.Shifts.Where(s => s.End < DateTime.Now).ToListAsync();
        foreach (var s in pastShifts)
        {
            db.TimeEntries.Add(new TimeEntry
            {
                EmployeeId = s.EmployeeId,
                ShiftId = s.Id,
                ClockIn = s.Start.AddMinutes(rnd.Next(-10, 15)),
                ClockOut = s.End.AddMinutes(rnd.Next(-5, 20))
            });
            s.Status = ShiftStatus.Completed;
        }

        // Примерни заявки.
        db.LeaveRequests.Add(new LeaveRequest
        {
            EmployeeId = maria.Id, Type = LeaveType.Paid,
            StartDate = monday.AddDays(14), EndDate = monday.AddDays(18),
            Reason = "Семейна почивка", Status = RequestStatus.PendingManager
        });
        var ivanNextShift = await db.Shifts
            .Where(s => s.EmployeeId == ivan.Id && s.Start > DateTime.Now.AddDays(1))
            .OrderBy(s => s.Start).FirstOrDefaultAsync();
        var mariaSameDay = ivanNextShift == null ? null : await db.Shifts
            .FirstOrDefaultAsync(s => s.EmployeeId == maria.Id && s.Start.Date == ivanNextShift.Start.Date);
        if (ivanNextShift != null && mariaSameDay != null)
        {
            db.ShiftSwapRequests.Add(new ShiftSwapRequest
            {
                ShiftId = ivanNextShift.Id, RequesterId = ivan.Id, TargetEmployeeId = maria.Id,
                TargetShiftId = mariaSameDay.Id,
                Reason = "Имам час при лекар", Status = RequestStatus.PendingColleague
            });
        }
        await db.SaveChangesAsync();
    }

    private static Shift NewShift(Employee e, Department d, DateTime start, DateTime end) =>
        new() { EmployeeId = e.Id, DepartmentId = d.Id, Start = start, End = end, BreakMinutes = 30 };

    public static DateTime WeekStart(DateTime date)
    {
        int diff = ((int)date.DayOfWeek + 6) % 7;
        return date.Date.AddDays(-diff);
    }
}
