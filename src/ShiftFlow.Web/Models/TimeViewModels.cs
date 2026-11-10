using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShiftFlow.Data.Entities;
using ShiftFlow.Services.Common;

namespace ShiftFlow.Web.Models;

public class MyTimeViewModel
{
    public TimeEntry? OpenEntry { get; set; }
    public Shift? CurrentShift { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public List<TimeEntry> Entries { get; set; } = new();
    public double TotalHours => Entries.Sum(e => e.WorkedHours);
}

public class TimeManageViewModel
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public string? EmployeeId { get; set; }
    public int? DepartmentId { get; set; }
    public SelectList? Employees { get; set; }
    public SelectList? Departments { get; set; }
    public List<TimeEntry> Entries { get; set; } = new();
}

public class TimeEntryFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Изберете служител.")]
    [Display(Name = "Служител")]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    [Display(Name = "Вход")]
    public DateTime ClockIn { get; set; } = DateTime.Today.AddHours(7);

    [Display(Name = "Изход")]
    public DateTime? ClockOut { get; set; } = DateTime.Today.AddHours(15);

    [Display(Name = "Смяна")]
    public int? ShiftId { get; set; }

    [StringLength(300)]
    [Display(Name = "Бележка / причина за корекцията")]
    public string? Note { get; set; }

    public SelectList? Employees { get; set; }
    public SelectList? Shifts { get; set; }
}

public class ReportViewModel
{
    [DataType(DataType.Date)]
    [Display(Name = "От")]
    public DateTime From { get; set; }

    [DataType(DataType.Date)]
    [Display(Name = "До")]
    public DateTime To { get; set; }

    [Display(Name = "Отдел")]
    public int? DepartmentId { get; set; }

    public SelectList? Departments { get; set; }
    public List<ReportRow> Rows { get; set; } = new();
}
