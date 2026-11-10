using Microsoft.AspNetCore.Identity;

namespace ShiftFlow.Data.Entities;

/// <summary>Служител. Наследява IdentityUser, за да служи и като потребителски акаунт.</summary>
public class Employee : IdentityUser
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public DateTime HireDate { get; set; } = DateTime.Today;
    public bool IsActive { get; set; } = true;

    /// <summary>Договорени часове седмично (напр. 40).</summary>
    public int WeeklyHours { get; set; } = 40;

    public int? DepartmentId { get; set; }
    public Department? Department { get; set; }

    public int? PositionId { get; set; }
    public Position? Position { get; set; }

    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
    public ICollection<LeaveRequest> LeaveRequests { get; set; } = new List<LeaveRequest>();
    public ICollection<TimeEntry> TimeEntries { get; set; } = new List<TimeEntry>();

    public string FullName => $"{FirstName} {LastName}";
}
