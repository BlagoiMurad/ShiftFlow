using System.ComponentModel.DataAnnotations;

namespace ShiftFlow.Web.Models;

public class DepartmentFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Въведете име.")]
    [StringLength(100)]
    [Display(Name = "Име")]
    public string Name { get; set; } = string.Empty;

    [StringLength(300)]
    [Display(Name = "Описание")]
    public string? Description { get; set; }

    [Range(1, 50, ErrorMessage = "Между 1 и 50.")]
    [Display(Name = "Минимум служители на смяна")]
    public int MinStaffPerShift { get; set; } = 1;
}

public class PositionFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Въведете име.")]
    [StringLength(100)]
    [Display(Name = "Длъжност")]
    public string Name { get; set; } = string.Empty;

    [Range(0, 1000, ErrorMessage = "Невалидна ставка.")]
    [Display(Name = "Часова ставка (лв.)")]
    public decimal HourlyRate { get; set; }
}
