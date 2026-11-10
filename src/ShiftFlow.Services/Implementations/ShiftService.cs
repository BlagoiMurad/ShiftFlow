using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Data.Entities;
using ShiftFlow.Data.Enums;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Interfaces;

namespace ShiftFlow.Services.Implementations;

public class ShiftService : IShiftService
{
    /// <summary>Максимална продължителност на една смяна.</summary>
    public const int MaxShiftHours = 12;
    /// <summary>Минимална почивка между две смени (междудневна почивка).</summary>
    public const int MinRestHours = 11;

    private readonly ApplicationDbContext _db;
    public ShiftService(ApplicationDbContext db) => _db = db;

    public Task<List<Shift>> GetForPeriodAsync(DateTime from, DateTime to, int? departmentId = null, string? employeeId = null)
    {
        var q = _db.Shifts.Include(s => s.Employee).Include(s => s.Department)
            .Where(s => s.Start < to && s.End > from);
        if (departmentId.HasValue) q = q.Where(s => s.DepartmentId == departmentId);
        if (!string.IsNullOrEmpty(employeeId)) q = q.Where(s => s.EmployeeId == employeeId);
        return q.OrderBy(s => s.Start).ToListAsync();
    }

    public Task<Shift?> GetByIdAsync(int id) =>
        _db.Shifts.Include(s => s.Employee).Include(s => s.Department).FirstOrDefaultAsync(s => s.Id == id);

    public async Task<string?> ValidateAsync(Shift shift, IEnumerable<int>? ignoreShiftIds = null)
    {
        var ignored = (ignoreShiftIds ?? Enumerable.Empty<int>()).Append(shift.Id).ToList();
        if (shift.End <= shift.Start)
            return "Краят на смяната трябва да е след началото.";
        if ((shift.End - shift.Start).TotalHours > MaxShiftHours)
            return $"Смяната не може да е по-дълга от {MaxShiftHours} часа.";
        if (shift.BreakMinutes < 0 || shift.BreakMinutes >= (shift.End - shift.Start).TotalMinutes)
            return "Почивката е невалидна.";

        var employee = await _db.Users.FindAsync(shift.EmployeeId);
        if (employee == null) return "Служителят не е намерен.";
        if (!employee.IsActive) return "Служителят е неактивен.";

        // Застъпване или недостатъчна почивка спрямо други смени на същия служител.
        var restWindowStart = shift.Start.AddHours(-MinRestHours);
        var restWindowEnd = shift.End.AddHours(MinRestHours);
        var neighbours = await _db.Shifts
            .Where(s => s.EmployeeId == shift.EmployeeId && !ignored.Contains(s.Id) && s.Status != ShiftStatus.Cancelled
                        && s.Start < restWindowEnd && s.End > restWindowStart)
            .ToListAsync();

        if (neighbours.Any(s => s.Start < shift.End && s.End > shift.Start))
            return $"{employee.FullName} вече има смяна в този интервал.";
        if (neighbours.Any())
            return $"{employee.FullName} трябва да има поне {MinRestHours} часа почивка между смените.";

        // Одобрен отпуск в този период.
        var onLeave = await _db.LeaveRequests.AnyAsync(l =>
            l.EmployeeId == shift.EmployeeId && l.Status == RequestStatus.Approved &&
            l.StartDate <= shift.End.Date && l.EndDate >= shift.Start.Date);
        if (onLeave)
            return $"{employee.FullName} е в одобрен отпуск в този период.";

        return null;
    }

    public async Task<ServiceResult> CreateAsync(Shift shift)
    {
        var error = await ValidateAsync(shift);
        if (error != null) return ServiceResult.Fail(error);
        _db.Shifts.Add(shift);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateAsync(Shift shift)
    {
        var existing = await _db.Shifts.FindAsync(shift.Id);
        if (existing == null) return ServiceResult.Fail("Смяната не е намерена.");

        if (shift.Status != ShiftStatus.Cancelled)
        {
            var error = await ValidateAsync(shift);
            if (error != null) return ServiceResult.Fail(error);
        }

        existing.EmployeeId = shift.EmployeeId;
        existing.DepartmentId = shift.DepartmentId;
        existing.Start = shift.Start;
        existing.End = shift.End;
        existing.BreakMinutes = shift.BreakMinutes;
        existing.Notes = shift.Notes;
        existing.Status = shift.Status;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var existing = await _db.Shifts.FindAsync(id);
        if (existing == null) return ServiceResult.Fail("Смяната не е намерена.");
        if (await _db.TimeEntries.AnyAsync(t => t.ShiftId == id))
            return ServiceResult.Fail("По смяната има отчетено време. Вместо изтриване я маркирайте като отменена.");
        _db.Shifts.Remove(existing);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult<int>> CopyWeekAsync(DateTime sourceWeekStart, int? departmentId)
    {
        var from = sourceWeekStart.Date;
        var to = from.AddDays(7);
        var source = await _db.Shifts
            .Where(s => s.Start >= from && s.Start < to && s.Status != ShiftStatus.Cancelled)
            .Where(s => !departmentId.HasValue || s.DepartmentId == departmentId)
            .AsNoTracking()
            .ToListAsync();

        if (source.Count == 0) return ServiceResult<int>.Fail("В избраната седмица няма смени за копиране.");

        int copied = 0;
        foreach (var s in source)
        {
            var copy = new Shift
            {
                EmployeeId = s.EmployeeId,
                DepartmentId = s.DepartmentId,
                Start = s.Start.AddDays(7),
                End = s.End.AddDays(7),
                BreakMinutes = s.BreakMinutes,
                Notes = s.Notes
            };
            // Пропускаме смени, които биха нарушили правилата (напр. отпуск следващата седмица).
            if (await ValidateAsync(copy) == null)
            {
                _db.Shifts.Add(copy);
                await _db.SaveChangesAsync();
                copied++;
            }
        }
        return ServiceResult<int>.Ok(copied);
    }
}
