using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Data.Entities;
using ShiftFlow.Data.Enums;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Interfaces;

namespace ShiftFlow.Services.Implementations;

public class TimeTrackingService : ITimeTrackingService
{
    /// <summary>Колко по-рано от началото на смяната може да се отбележи вход.</summary>
    public const int EarlyClockInMinutes = 60;

    private readonly ApplicationDbContext _db;
    public TimeTrackingService(ApplicationDbContext db) => _db = db;

    public Task<TimeEntry?> GetOpenEntryAsync(string employeeId) =>
        _db.TimeEntries.Include(t => t.Shift)
            .FirstOrDefaultAsync(t => t.EmployeeId == employeeId && t.ClockOut == null);

    public async Task<ServiceResult<TimeEntry>> ClockInAsync(string employeeId, DateTime? now = null)
    {
        var time = now ?? DateTime.Now;
        if (await GetOpenEntryAsync(employeeId) != null)
            return ServiceResult<TimeEntry>.Fail("Вече сте отбелязали вход.");

        // Свързваме отчитането с текущата смяна, ако има такава.
        var windowStart = time.AddMinutes(EarlyClockInMinutes);
        var shift = await _db.Shifts
            .Where(s => s.EmployeeId == employeeId && s.Status == ShiftStatus.Planned &&
                        s.Start <= windowStart && s.End > time)
            .OrderBy(s => s.Start)
            .FirstOrDefaultAsync();

        var entry = new TimeEntry { EmployeeId = employeeId, ShiftId = shift?.Id, ClockIn = time };
        _db.TimeEntries.Add(entry);
        await _db.SaveChangesAsync();
        return ServiceResult<TimeEntry>.Ok(entry);
    }

    public async Task<ServiceResult<TimeEntry>> ClockOutAsync(string employeeId, string? note, DateTime? now = null)
    {
        var entry = await GetOpenEntryAsync(employeeId);
        if (entry == null) return ServiceResult<TimeEntry>.Fail("Не сте отбелязали вход.");

        entry.ClockOut = now ?? DateTime.Now;
        entry.Note = note;
        if (entry.Shift != null && entry.ClockOut >= entry.Shift.End.AddMinutes(-30))
            entry.Shift.Status = ShiftStatus.Completed;

        await _db.SaveChangesAsync();
        return ServiceResult<TimeEntry>.Ok(entry);
    }

    public Task<List<TimeEntry>> GetEntriesAsync(DateTime from, DateTime to, string? employeeId = null, int? departmentId = null)
    {
        var q = _db.TimeEntries.Include(t => t.Employee).ThenInclude(e => e.Department).Include(t => t.Shift)
            .Where(t => t.ClockIn >= from && t.ClockIn < to);
        if (!string.IsNullOrEmpty(employeeId)) q = q.Where(t => t.EmployeeId == employeeId);
        if (departmentId.HasValue) q = q.Where(t => t.Employee.DepartmentId == departmentId);
        return q.OrderByDescending(t => t.ClockIn).ToListAsync();
    }

    public Task<TimeEntry?> GetByIdAsync(int id) =>
        _db.TimeEntries.Include(t => t.Employee).Include(t => t.Shift).FirstOrDefaultAsync(t => t.Id == id);

    public async Task<ServiceResult> SaveCorrectionAsync(TimeEntry entry)
    {
        if (entry.ClockOut.HasValue && entry.ClockOut <= entry.ClockIn)
            return ServiceResult.Fail("Изходът трябва да е след входа.");
        if (entry.ClockOut.HasValue && (entry.ClockOut.Value - entry.ClockIn).TotalHours > 24)
            return ServiceResult.Fail("Един запис не може да е по-дълъг от 24 часа.");

        bool overlaps = await _db.TimeEntries.AnyAsync(t => t.EmployeeId == entry.EmployeeId && t.Id != entry.Id &&
            t.ClockIn < (entry.ClockOut ?? DateTime.MaxValue) && (t.ClockOut ?? DateTime.MaxValue) > entry.ClockIn);
        if (overlaps) return ServiceResult.Fail("Записът се застъпва с друг запис на служителя.");

        if (entry.Id == 0)
        {
            entry.IsManualCorrection = true;
            _db.TimeEntries.Add(entry);
        }
        else
        {
            var existing = await _db.TimeEntries.FindAsync(entry.Id);
            if (existing == null) return ServiceResult.Fail("Записът не е намерен.");
            existing.ClockIn = entry.ClockIn;
            existing.ClockOut = entry.ClockOut;
            existing.ShiftId = entry.ShiftId;
            existing.Note = entry.Note;
            existing.IsManualCorrection = true;
        }
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }

    public async Task<ServiceResult> DeleteAsync(int id)
    {
        var existing = await _db.TimeEntries.FindAsync(id);
        if (existing == null) return ServiceResult.Fail("Записът не е намерен.");
        _db.TimeEntries.Remove(existing);
        await _db.SaveChangesAsync();
        return ServiceResult.Ok();
    }
}
