namespace ShiftFlow.Data.Entities;

/// <summary>Длъжност (касиер, складов работник, управител и т.н.).</summary>
public class Position
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal HourlyRate { get; set; }

    public ICollection<Employee> Employees { get; set; } = new List<Employee>();
}
