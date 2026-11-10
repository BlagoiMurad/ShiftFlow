using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Data.Entities;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Interfaces;

namespace ShiftFlow.Services.Implementations;

public class EmployeeService : IEmployeeService
{
    private readonly ApplicationDbContext _db;
    private readonly UserManager<Employee> _users;

    public EmployeeService(ApplicationDbContext db, UserManager<Employee> users)
    {
        _db = db;
        _users = users;
    }

    public Task<List<Employee>> GetAllAsync(int? departmentId = null, bool onlyActive = false)
    {
        var q = _db.Users.Include(e => e.Department).Include(e => e.Position).AsQueryable();
        if (departmentId.HasValue) q = q.Where(e => e.DepartmentId == departmentId);
        if (onlyActive) q = q.Where(e => e.IsActive);
        return q.OrderBy(e => e.FirstName).ThenBy(e => e.LastName).ToListAsync();
    }

    public Task<Employee?> GetByIdAsync(string id) =>
        _db.Users.Include(e => e.Department).Include(e => e.Position).FirstOrDefaultAsync(e => e.Id == id);

    public async Task<string?> GetRoleAsync(Employee employee) =>
        (await _users.GetRolesAsync(employee)).FirstOrDefault();

    public async Task<ServiceResult<string>> CreateAsync(Employee employee, string password, string role)
    {
        if (!Roles.All.Contains(role)) return ServiceResult<string>.Fail("Невалидна роля.");
        employee.UserName = employee.Email;
        employee.EmailConfirmed = true;

        var result = await _users.CreateAsync(employee, password);
        if (!result.Succeeded)
            return ServiceResult<string>.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));

        await _users.AddToRoleAsync(employee, role);
        return ServiceResult<string>.Ok(employee.Id);
    }

    public async Task<ServiceResult> UpdateAsync(Employee employee, string role)
    {
        if (!Roles.All.Contains(role)) return ServiceResult.Fail("Невалидна роля.");
        var existing = await _users.FindByIdAsync(employee.Id);
        if (existing == null) return ServiceResult.Fail("Служителят не е намерен.");

        existing.FirstName = employee.FirstName;
        existing.LastName = employee.LastName;
        existing.Email = employee.Email;
        existing.UserName = employee.Email;
        existing.PhoneNumber = employee.PhoneNumber;
        existing.DepartmentId = employee.DepartmentId;
        existing.PositionId = employee.PositionId;
        existing.HireDate = employee.HireDate;
        existing.WeeklyHours = employee.WeeklyHours;

        var result = await _users.UpdateAsync(existing);
        if (!result.Succeeded)
            return ServiceResult.Fail(string.Join(" ", result.Errors.Select(e => e.Description)));

        var currentRoles = await _users.GetRolesAsync(existing);
        if (!currentRoles.Contains(role))
        {
            await _users.RemoveFromRolesAsync(existing, currentRoles);
            await _users.AddToRoleAsync(existing, role);
        }
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> SetActiveAsync(string id, bool isActive)
    {
        var existing = await _users.FindByIdAsync(id);
        if (existing == null) return ServiceResult.Fail("Служителят не е намерен.");
        existing.IsActive = isActive;
        // Неактивен служител не може да влиза в системата.
        existing.LockoutEnabled = true;
        existing.LockoutEnd = isActive ? null : DateTimeOffset.MaxValue;
        await _users.UpdateAsync(existing);
        return ServiceResult.Ok();
    }
}
