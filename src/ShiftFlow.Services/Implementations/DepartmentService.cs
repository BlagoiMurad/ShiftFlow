using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Data.Entities;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Interfaces;

namespace ShiftFlow.Services.Implementations;

public class DepartmentService : IDepartmentService
{
    private readonly ApplicationDbContext _db;
    public DepartmentService(ApplicationDbContext db) => _db = db;

    public Task<List<Department>> GetAllAsync() =>
        _db.Departments.Include(d => d.Employees).OrderBy(d => d.Name).ToListAsync();

    public Task<Department?> GetByIdAsync(int id) =>
        _db.Departments.Include(d => d.Employees).FirstOrDefaultAsync(d => d.Id == id);

    public async Task<ServiceResult> CreateAsync(Department department)
    {
        if (await _db.Departments.AnyAsync(d => d.Name == department.Name))
            return ServiceResult.Fail("Вече има отдел с това име.");
        _db.Departments.Add(department);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateAsync(Department department)
    {
        var existing = await _db.Departments.FindAsync(department.Id);
        if (existing == null) return ServiceResult.Fail("Отделът не е намерен.");
        if (await _db.Departments.AnyAsync(d => d.Name == department.Name && d.Id != department.Id))
            return ServiceResult.Fail("Вече има отдел с това име.");

        existing.Name = department.Name;
        existing.Description = department.Description;
        existing.MinStaffPerShift = department.MinStaffPerShift;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var existing = await _db.Departments.FindAsync(id);
        if (existing == null) return ServiceResult.Fail("Отделът не е намерен.");
        if (await _db.Shifts.AnyAsync(s => s.DepartmentId == id))
            return ServiceResult.Fail("Отделът има планирани смени и не може да бъде изтрит.");
        _db.Departments.Remove(existing);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
