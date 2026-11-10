using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShiftFlow.Data.Entities;
using ShiftFlow.Data.Enums;

namespace ShiftFlow.Web.Models;

public class SwapRequestFormViewModel
{
    [Required(ErrorMessage = "Изберете смяна.")]
    [Display(Name = "Моята смяна")]
    public int ShiftId { get; set; }

    [Required(ErrorMessage = "Изберете колега.")]
    [Display(Name = "Колега")]
    public string TargetEmployeeId { get; set; } = string.Empty;

    [Display(Name = "Смяна на колегата (по желание, за размяна)")]
    public int? TargetShiftId { get; set; }

    [StringLength(500)]
    [Display(Name = "Причина")]
    public string? Reason { get; set; }

    public SelectList? MyShifts { get; set; }
    public SelectList? Colleagues { get; set; }
    /// <summary>Предстоящи смени на колегите: за избор на смяна за размяна.</summary>
    public List<Shift> ColleagueShifts { get; set; } = new();
}

public class SwapRequestIndexViewModel
{
    public string CurrentUserId { get; set; } = string.Empty;
    public List<ShiftSwapRequest> Requests { get; set; } = new();
}

public class LeaveRequestFormViewModel
{
    [Display(Name = "Вид отпуск")]
    public LeaveType Type { get; set; } = LeaveType.Paid;

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "От дата")]
    public DateTime StartDate { get; set; } = DateTime.Today.AddDays(7);

    [Required]
    [DataType(DataType.Date)]
    [Display(Name = "До дата")]
    public DateTime EndDate { get; set; } = DateTime.Today.AddDays(7);

    [StringLength(500)]
    [Display(Name = "Причина")]
    public string? Reason { get; set; }

    public int PaidDaysLeft { get; set; }
}

public class LeaveIndexViewModel
{
    public List<LeaveRequest> Requests { get; set; } = new();
    public int PaidDaysUsed { get; set; }
    public int PaidDaysLeft { get; set; }
}

public class LeaveManageViewModel
{
    public RequestStatus? Status { get; set; }
    public List<LeaveRequest> Requests { get; set; } = new();
}

/// <summary>Решение на управител по заявка (одобряване / отхвърляне).</summary>
public class ReviewViewModel
{
    public int Id { get; set; }
    public bool Approve { get; set; }

    [StringLength(500)]
    [Display(Name = "Коментар")]
    public string? Comment { get; set; }
}
