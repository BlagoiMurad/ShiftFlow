using ShiftFlow.Data.Enums;

namespace ShiftFlow.Data.Entities;

/// <summary>Планирана работна смяна на служител.</summary>
public class Shift
{
    public int Id { get; set; }

    public string EmployeeId { get; set; } = string.Empty;
    public Employee Employee { get; set; } = null!;

    public int DepartmentId { get; set; }
    public Department Department { get; set; } = null!;

    public DateTime Start { get; set; }
    public DateTime End { get; set; }

    /// <summary>Почивка в минути, която не се брои за работно време.</summary>
    public int BreakMinutes { get; set; }

    public string? Notes { get; set; }
    public ShiftStatus Status { get; set; } = ShiftStatus.Planned;

    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();

    public double PlannedHours => Math.Max(0, (End - Start).TotalHours - BreakMinutes / 60.0);
}
