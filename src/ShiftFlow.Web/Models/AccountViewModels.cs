using System.ComponentModel.DataAnnotations;

namespace ShiftFlow.Web.Models;

public class LoginViewModel
{
    [Required(ErrorMessage = "Въведете имейл.")]
    [EmailAddress(ErrorMessage = "Невалиден имейл.")]
    [Display(Name = "Имейл")]
    public string Email { get; set; } = string.Empty;

    [Required(ErrorMessage = "Въведете парола.")]
    [DataType(DataType.Password)]
    [Display(Name = "Парола")]
    public string Password { get; set; } = string.Empty;

    [Display(Name = "Запомни ме")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Задължително поле.")]
    [DataType(DataType.Password)]
    [Display(Name = "Текуща парола")]
    public string CurrentPassword { get; set; } = string.Empty;

    [Required(ErrorMessage = "Задължително поле.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Паролата трябва да е поне 6 символа.")]
    [DataType(DataType.Password)]
    [Display(Name = "Нова парола")]
    public string NewPassword { get; set; } = string.Empty;

    [Compare(nameof(NewPassword), ErrorMessage = "Паролите не съвпадат.")]
    [DataType(DataType.Password)]
    [Display(Name = "Повторете новата парола")]
    public string ConfirmPassword { get; set; } = string.Empty;
}
