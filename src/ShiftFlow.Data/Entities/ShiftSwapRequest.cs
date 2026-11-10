using ShiftFlow.Data.Enums;

namespace ShiftFlow.Data.Entities;

/// <summary>
/// Заявка за размяна на смяна. Служителят предлага своя смяна на колега;
/// колегата приема, след което управител одобрява.
/// Ако е посочена TargetShift, двете смени се разменят, иначе смяната просто се прехвърля.
/// </summary>
public class ShiftSwapRequest
{
    public int Id { get; set; }

    public int ShiftId { get; set; }
    public Shift Shift { get; set; } = null!;

    public string RequesterId { get; set; } = string.Empty;
    public Employee Requester { get; set; } = null!;

    public string TargetEmployeeId { get; set; } = string.Empty;
    public Employee TargetEmployee { get; set; } = null!;

    public int? TargetShiftId { get; set; }
    public Shift? TargetShift { get; set; }

    public string? Reason { get; set; }
    public RequestStatus Status { get; set; } = RequestStatus.PendingColleague;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? ReviewedById { get; set; }
    public Employee? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ManagerComment { get; set; }
}
