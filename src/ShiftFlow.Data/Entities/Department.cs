namespace ShiftFlow.Data.Entities;

/// <summary>Отдел / обект, за който се планират смени.</summary>
public class Department
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }

    /// <summary>Минимален брой служители, които трябва да са на смяна едновременно.</summary>
    public int MinStaffPerShift { get; set; } = 1;

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
    public ICollection<Shift> Shifts { get; set; } = new List<Shift>();
}
