using Microsoft.EntityFrameworkCore;
using ShiftFlow.Data;
using ShiftFlow.Data.Enums;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Interfaces;

namespace ShiftFlow.Services.Implementations;

public class ReportService : IReportService
{
    /// <summary>Закъснение над толкова минути се отчита като закъснение.</summary>
    public const int LateToleranceMinutes = 5;

    private readonly ApplicationDbContext _db;
    public ReportService(ApplicationDbContext db) => _db = db;

    public async Task<List<ReportRow>> GetHoursReportAsync(DateTime from, DateTime to, int? departmentId = null)
    {
        var employees = await _db.Users.Include(e => e.Department).Include(e => e.Position)
            .Where(e => e.DepartmentId != null && (!departmentId.HasValue || e.DepartmentId == departmentId))
            .OrderBy(e => e.FirstName).ToListAsync();
        var ids = employees.Select(e => e.Id).ToList();

        // За сравнението се броят само смени, които вече са приключили.
        var cutoff = to < DateTime.Now ? to : DateTime.Now;
        var shifts = await _db.Shifts
            .Where(s => ids.Contains(s.EmployeeId) && s.Status != ShiftStatus.Cancelled && s.Start >= from && s.Start < to && s.End <= cutoff)
            .ToListAsync();
        var entries = await _db.TimeEntries.Include(t => t.Shift)
            .Where(t => ids.Contains(t.EmployeeId) && t.ClockIn >= from && t.ClockIn < to)
            .ToListAsync();
        var leaves = await _db.LeaveRequests
            .Where(l => ids.Contains(l.EmployeeId) && l.Status == RequestStatus.Approved &&
                        l.StartDate < to && l.EndDate >= from)
            .ToListAsync();

        return employees.Select(e =>
        {
            var myEntries = entries.Where(t => t.EmployeeId == e.Id).ToList();
            var worked = myEntries.Sum(t => t.WorkedHours);
            return new ReportRow
            {
                EmployeeId = e.Id,
                EmployeeName = e.FullName,
                DepartmentName = e.Department?.Name,
                ShiftCount = shifts.Count(s => s.EmployeeId == e.Id),
                PlannedHours = Math.Round(shifts.Where(s => s.EmployeeId == e.Id).Sum(s => s.PlannedHours), 2),
                WorkedHours = Math.Round(worked, 2),
                LateArrivals = myEntries.Count(t => t.Shift != null && t.ClockIn > t.Shift.Start.AddMinutes(LateToleranceMinutes)),
                LeaveDays = leaves.Where(l => l.EmployeeId == e.Id).Sum(l =>
                {
                    var start = l.StartDate < from ? from.Date : l.StartDate;
                    var end = l.EndDate >= to ? to.Date.AddDays(-1) : l.EndDate;
                    return (end - start).Days + 1;
                }),
                EstimatedPay = Math.Round((decimal)worked * (e.Position?.HourlyRate ?? 0), 2)
            };
        }).ToList();
    }

    public async Task<DashboardStats> GetDashboardStatsAsync()
    {
        var today = DateTime.Today;
        var tomorrow = today.AddDays(1);
        return new DashboardStats
        {
            EmployeesCount = await _db.Users.CountAsync(e => e.IsActive),
            ShiftsToday = await _db.Shifts.CountAsync(s => s.Status != ShiftStatus.Cancelled && s.Start >= today && s.Start < tomorrow),
            ClockedInNow = await _db.TimeEntries.CountAsync(t => t.ClockOut == null),
            PendingSwapRequests = await _db.ShiftSwapRequests.CountAsync(r => r.Status == RequestStatus.PendingManager),
            PendingLeaveRequests = await _db.LeaveRequests.CountAsync(l => l.Status == RequestStatus.PendingManager),
            OnLeaveToday = await _db.LeaveRequests.CountAsync(l => l.Status == RequestStatus.Approved && l.StartDate <= today && l.EndDate >= today)
        };
    }
}
