using System.Diagnostics;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShiftFlow.Data.Enums;
using ShiftFlow.Services.Implementations;
using ShiftFlow.Services.Interfaces;
using ShiftFlow.Web.Data;
using ShiftFlow.Web.Helpers;
using ShiftFlow.Web.Models;

namespace ShiftFlow.Web.Controllers;

[Authorize]
public class HomeController : Controller
{
    private readonly IReportService _reports;
    private readonly IShiftService _shifts;
    private readonly ITimeTrackingService _time;
    private readonly ISwapRequestService _swaps;
    private readonly ILeaveService _leaves;
    private readonly IEmployeeService _employees;

    public HomeController(IReportService reports, IShiftService shifts, ITimeTrackingService time,
        ISwapRequestService swaps, ILeaveService leaves, IEmployeeService employees)
    {
        _reports = reports;
        _shifts = shifts;
        _time = time;
        _swaps = swaps;
        _leaves = leaves;
        _employees = employees;
    }

    /// <summary>Табло: обобщение за управителя и лична информация за служителя.</summary>
    public async Task<IActionResult> Index()
    {
        var userId = User.UserId();
        var me = await _employees.GetByIdAsync(userId);
        var weekStart = DbSeeder.WeekStart(DateTime.Today);

        var myWeekShifts = await _shifts.GetForPeriodAsync(weekStart, weekStart.AddDays(7), employeeId: userId);
        var myWeekEntries = await _time.GetEntriesAsync(weekStart, weekStart.AddDays(7), userId);
        var swaps = await _swaps.GetForEmployeeAsync(userId);

        var model = new DashboardViewModel
        {
            FullName = me?.FullName ?? User.Identity?.Name ?? "",
            IsManager = User.IsManager(),
            OpenEntry = await _time.GetOpenEntryAsync(userId),
            MyUpcomingShifts = (await _shifts.GetForPeriodAsync(DateTime.Now, DateTime.Today.AddDays(14), employeeId: userId))
                .Where(s => s.Status == ShiftStatus.Planned).Take(5).ToList(),
            MyPendingColleagueRequests = swaps.Count(r => r.TargetEmployeeId == userId && r.Status == RequestStatus.PendingColleague),
            MyWeekPlannedHours = myWeekShifts.Where(s => s.Status != ShiftStatus.Cancelled).Sum(s => s.PlannedHours),
            MyWeekWorkedHours = myWeekEntries.Sum(e => e.WorkedHours),
            PaidLeaveDaysLeft = LeaveService.AnnualPaidLeaveDays - await _leaves.GetUsedPaidDaysAsync(userId, DateTime.Today.Year)
        };

        if (model.IsManager)
        {
            model.Stats = await _reports.GetDashboardStatsAsync();
            model.TodayShifts = (await _shifts.GetForPeriodAsync(DateTime.Today, DateTime.Today.AddDays(1)))
                .Where(s => s.Status != ShiftStatus.Cancelled).ToList();
        }

        return View(model);
    }

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
}
