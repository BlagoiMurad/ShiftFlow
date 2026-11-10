using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShiftFlow.Data.Entities;
using ShiftFlow.Data.Enums;

namespace ShiftFlow.Web.Models;

public class ShiftFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Изберете служител.")]
    [Display(Name = "Служител")]
    public string EmployeeId { get; set; } = string.Empty;

    [Required(ErrorMessage = "Изберете отдел.")]
    [Display(Name = "Отдел")]
    public int DepartmentId { get; set; }

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "Дата")]
    public DateTime Date { get; set; } = DateTime.Today;

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "Начало")]
    public TimeSpan StartTime { get; set; } = new(7, 0, 0);

    [Required]
    [DataType(DataType.Time)]
    [Display(Name = "Край")]
    public TimeSpan EndTime { get; set; } = new(15, 0, 0);

    [Range(0, 240, ErrorMessage = "Между 0 и 240 минути.")]
    [Display(Name = "Почивка (мин.)")]
    public int BreakMinutes { get; set; } = 30;

    [StringLength(500)]
    [Display(Name = "Бележки")]
    public string? Notes { get; set; }

    [Display(Name = "Статус")]
    public ShiftStatus Status { get; set; } = ShiftStatus.Planned;

    public SelectList? Employees { get; set; }
    public SelectList? Departments { get; set; }

    /// <summary>Нощна смяна: краят е на следващия ден.</summary>
    public DateTime StartDateTime => Date.Date + StartTime;
    public DateTime EndDateTime => EndTime > StartTime ? Date.Date + EndTime : Date.Date.AddDays(1) + EndTime;
}

/// <summary>Седмичен график: редове = служители, колони = дни.</summary>
public class WeekScheduleViewModel
{
    public DateTime WeekStart { get; set; }
    public int? DepartmentId { get; set; }
    public SelectList? Departments { get; set; }
    public List<Employee> Employees { get; set; } = new();
    public List<Shift> Shifts { get; set; } = new();
    public List<LeaveRequest> Leaves { get; set; } = new();
    public bool CanEdit { get; set; }

    public IEnumerable<DateTime> Days => Enumerable.Range(0, 7).Select(i => WeekStart.AddDays(i));

    public IEnumerable<Shift> ShiftsFor(string employeeId, DateTime day) =>
        Shifts.Where(s => s.EmployeeId == employeeId && s.Start.Date == day.Date);

    public bool IsOnLeave(string employeeId, DateTime day) =>
        Leaves.Any(l => l.EmployeeId == employeeId && l.StartDate <= day && l.EndDate >= day);

    public double TotalHours(string employeeId) =>
        Shifts.Where(s => s.EmployeeId == employeeId && s.Status != ShiftStatus.Cancelled).Sum(s => s.PlannedHours);
}

public class MyShiftsViewModel
{
    public List<Shift> Upcoming { get; set; } = new();
    public List<Shift> Past { get; set; } = new();
}
