using System.Security.Claims;
using ShiftFlow.Data.Enums;

namespace ShiftFlow.Web.Helpers;

/// <summary>Български надписи и CSS класове за изброимите типове.</summary>
public static class DisplayHelpers
{
    public static string ToBg(this ShiftStatus s) => s switch
    {
        ShiftStatus.Planned => "Планирана",
        ShiftStatus.Completed => "Отработена",
        ShiftStatus.Cancelled => "Отменена",
        _ => s.ToString()
    };

    public static string ToBg(this RequestStatus s) => s switch
    {
        RequestStatus.PendingColleague => "Чака колега",
        RequestStatus.PendingManager => "Чака управител",
        RequestStatus.Approved => "Одобрена",
        RequestStatus.Rejected => "Отхвърлена",
        RequestStatus.Declined => "Отказана от колега",
        RequestStatus.Cancelled => "Оттеглена",
        _ => s.ToString()
    };

    public static string ToBg(this LeaveType t) => t switch
    {
        LeaveType.Paid => "Платен",
        LeaveType.Sick => "Болничен",
        LeaveType.Unpaid => "Неплатен",
        _ => t.ToString()
    };

    public static string BadgeClass(this RequestStatus s) => s switch
    {
        RequestStatus.Approved => "bg-success",
        RequestStatus.Rejected or RequestStatus.Declined => "bg-danger",
        RequestStatus.Cancelled => "bg-secondary",
        _ => "bg-warning text-dark"
    };

    public static string BadgeClass(this ShiftStatus s) => s switch
    {
        ShiftStatus.Completed => "bg-success",
        ShiftStatus.Cancelled => "bg-secondary",
        _ => "bg-primary"
    };

    public static string RoleToBg(string? role) => role switch
    {
        "Admin" => "Администратор",
        "Manager" => "Управител",
        "Employee" => "Служител",
        _ => "-"
    };

    public static string DayName(DateTime d) => d.DayOfWeek switch
    {
        DayOfWeek.Monday => "Понеделник",
        DayOfWeek.Tuesday => "Вторник",
        DayOfWeek.Wednesday => "Сряда",
        DayOfWeek.Thursday => "Четвъртък",
        DayOfWeek.Friday => "Петък",
        DayOfWeek.Saturday => "Събота",
        _ => "Неделя"
    };

    public static string Hours(double h) => h.ToString("0.##") + " ч.";

    public static string UserId(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

    public static bool IsManager(this ClaimsPrincipal user) =>
        user.IsInRole("Admin") || user.IsInRole("Manager");
}
