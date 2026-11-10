using ShiftFlow.Data.Enums;

namespace ShiftFlow.Data.Entities;

/// <summary>Заявка за отпуск.</summary>
public class LeaveRequest
{
    public int Id { get; set; }

    public string EmployeeId { get; set; } = string.Empty;
    public Employee Employee { get; set; } = null!;

    public LeaveType Type { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string? Reason { get; set; }

    public RequestStatus Status { get; set; } = RequestStatus.PendingManager;
    public DateTime CreatedAt { get; set; } = DateTime.Now;

    public string? ReviewedById { get; set; }
    public Employee? ReviewedBy { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public string? ManagerComment { get; set; }

    public int Days => (EndDate.Date - StartDate.Date).Days + 1;
}
