using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;

namespace ShiftFlow.Web.Models;

public class EmployeeListItemViewModel
{
    public string Id { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string? Department { get; set; }
    public string? Position { get; set; }
    public string? Role { get; set; }
    public bool IsActive { get; set; }
}

public class EmployeeIndexViewModel
{
    public List<EmployeeListItemViewModel> Employees { get; set; } = new();
    public int? DepartmentId { get; set; }
    public SelectList? Departments { get; set; }
}

public class EmployeeFormViewModel
{
    public string? Id { get; set; }

    [Required(ErrorMessage = "Въведете име.")]
    [StringLength(50)]
    [Display(Name = "Име")]
    public string FirstName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Въведете фамилия.")]
    [StringLength(50)]
    [Display(Name = "Фамилия")]
    public string LastName { get; set; } = string.Empty;

    [Required(ErrorMessage = "Въведете имейл.")]
    [EmailAddress(ErrorMessage = "Невалиден имейл.")]
    [Display(Name = "Имейл")]
    public string Email { get; set; } = string.Empty;

    [Phone(ErrorMessage = "Невалиден телефон.")]
    [Display(Name = "Телефон")]
    public string? PhoneNumber { get; set; }

    [Display(Name = "Отдел")]
    public int? DepartmentId { get; set; }

    [Display(Name = "Длъжност")]
    public int? PositionId { get; set; }

    [Required]
    [Display(Name = "Роля")]
    public string Role { get; set; } = "Employee";

    [DataType(DataType.Date)]
    [Display(Name = "Дата на назначаване")]
    public DateTime HireDate { get; set; } = DateTime.Today;

    [Range(1, 60, ErrorMessage = "Между 1 и 60.")]
    [Display(Name = "Часове седмично по договор")]
    public int WeeklyHours { get; set; } = 40;

    /// <summary>Само при създаване.</summary>
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Паролата трябва да е поне 6 символа.")]
    [DataType(DataType.Password)]
    [Display(Name = "Начална парола")]
    public string? Password { get; set; }

    public SelectList? Departments { get; set; }
    public SelectList? Positions { get; set; }
    public SelectList? Roles { get; set; }
}

public class EmployeeDetailsViewModel
{
    public EmployeeFormViewModel Employee { get; set; } = new();
    public string? DepartmentName { get; set; }
    public string? PositionName { get; set; }
    public bool IsActive { get; set; }
    public List<ShiftFlow.Data.Entities.Shift> UpcomingShifts { get; set; } = new();
    public List<ShiftFlow.Data.Entities.LeaveRequest> Leaves { get; set; } = new();
    public int PaidLeaveDaysLeft { get; set; }
}
