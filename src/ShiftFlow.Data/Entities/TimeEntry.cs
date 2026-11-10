namespace ShiftFlow.Data.Entities;

/// <summary>Реално отработено време: отбелязване на вход и изход.</summary>
public class TimeEntry
{
    public int Id { get; set; }

    public string EmployeeId { get; set; } = string.Empty;
    public Employee Employee { get; set; } = null!;

    /// <summary>Смяната, към която е свързано отчитането (ако има такава).</summary>
    public int? ShiftId { get; set; }
    public Shift? Shift { get; set; }

    public DateTime ClockIn { get; set; }
    public DateTime? ClockOut { get; set; }
    public string? Note { get; set; }

    /// <summary>Ръчно коригирано от управител.</summary>
    public bool IsManualCorrection { get; set; }

    public double WorkedHours => ClockOut.HasValue ? (ClockOut.Value - ClockIn).TotalHours : 0;
}
