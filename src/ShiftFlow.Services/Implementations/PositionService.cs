using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Data.Entities;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Interfaces;

namespace ShiftFlow.Services.Implementations;

public class PositionService : IPositionService
{
    private readonly ApplicationDbContext _db;
    public PositionService(ApplicationDbContext db) => _db = db;

    public Task<List<Position>> GetAllAsync() =>
        _db.Positions.Include(p => p.Employees).OrderBy(p => p.Name).ToListAsync();

    public Task<Position?> GetByIdAsync(int id) => _db.Positions.FirstOrDefaultAsync(p => p.Id == id);

    public async Task<ServiceResult> CreateAsync(Position position)
    {
        if (position.HourlyRate < 0) return ServiceResult.Fail("Ставката не може да е отрицателна.");
        _db.Positions.Add(position);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> UpdateAsync(Position position)
    {
        var existing = await _db.Positions.FindAsync(position.Id);
        if (existing == null) return ServiceResult.Fail("Длъжността не е намерена.");
        if (position.HourlyRate < 0) return ServiceResult.Fail("Ставката не може да е отрицателна.");
        existing.Name = position.Name;
        existing.HourlyRate = position.HourlyRate;
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var existing = await _db.Positions.FindAsync(id);
        if (existing == null) return ServiceResult.Fail("Длъжността не е намерена.");
        _db.Positions.Remove(existing);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
