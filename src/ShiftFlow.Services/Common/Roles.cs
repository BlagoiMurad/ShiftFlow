namespace ShiftFlow.Services.Common;

public static class Roles
{
    public const string Admin = "Admin";
    public const string Manager = "Manager";
    public const string Employee = "Employee";

    /// <summary>За [Authorize(Roles = ...)] — роли с права на управление.</summary>
    public const string AdminOrManager = Admin + "," + Manager;

    public static readonly string[] All = { Admin, Manager, Employee };
}
