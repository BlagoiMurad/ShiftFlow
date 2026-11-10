using ShiftFlow.Data.Entities;
using ShiftFlow.Services.Common;

namespace ShiftFlow.Web.Models;

public class DashboardViewModel
{
    public string FullName { get; set; } = string.Empty;
    public bool IsManager { get; set; }
    public DashboardStats? Stats { get; set; }
    public List<Shift> TodayShifts { get; set; } = new();
    public List<Shift> MyUpcomingShifts { get; set; } = new();
    public TimeEntry? OpenEntry { get; set; }
    public int MyPendingColleagueRequests { get; set; }
    public double MyWeekPlannedHours { get; set; }
    public double MyWeekWorkedHours { get; set; }
    public int PaidLeaveDaysLeft { get; set; }
}
