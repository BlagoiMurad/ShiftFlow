namespace ShiftFlow.Services.Common;

/// <summary>Ред от справката за планирано спрямо реално отработено време.</summary>
public class ReportRow
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string? DepartmentName { get; set; }
    public int ShiftCount { get; set; }
    public double PlannedHours { get; set; }
    public double WorkedHours { get; set; }
    public double Difference => Math.Round(WorkedHours - PlannedHours, 2);
    public int LateArrivals { get; set; }
    public int LeaveDays { get; set; }
    public decimal EstimatedPay { get; set; }
}

public class DashboardStats
{
    public int EmployeesCount { get; set; }
    public int ShiftsToday { get; set; }
    public int ClockedInNow { get; set; }
    public int PendingSwapRequests { get; set; }
    public int PendingLeaveRequests { get; set; }
    public int OnLeaveToday { get; set; }
}
