using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShiftFlow.Data.Entities;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Implementations;
using ShiftFlow.Services.Interfaces;
using ShiftFlow.Web.Helpers;
using ShiftFlow.Web.Models;

namespace ShiftFlow.Web.Controllers;

[Authorize(Roles = Roles.AdminOrManager)]
public class EmployeesController : Controller
{
    private readonly IEmployeeService _employees;
    private readonly IDepartmentService _departments;
    private readonly IPositionService _positions;
    private readonly IShiftService _shifts;
    private readonly ILeaveService _leaves;

    public EmployeesController(IEmployeeService employees, IDepartmentService departments, IPositionService positions,
        IShiftService shifts, ILeaveService leaves)
    {
        _employees = employees;
        _departments = departments;
        _positions = positions;
        _shifts = shifts;
        _leaves = leaves;
    }

    public async Task<IActionResult> Index(int? departmentId)
    {
        var list = await _employees.GetAllAsync(departmentId);
        var items = new List<EmployeeListItemViewModel>();
        foreach (var e in list)
        {
            items.Add(new EmployeeListItemViewModel
            {
                Id = e.Id, FullName = e.FullName, Email = e.Email ?? "",
                Department = e.Department?.Name, Position = e.Position?.Name,
                Role = await _employees.GetRoleAsync(e), IsActive = e.IsActive
            });
        }
        return View(new EmployeeIndexViewModel
        {
            Employees = items,
            DepartmentId = departmentId,
            Departments = new SelectList(await _departments.GetAllAsync(), "Id", "Name", departmentId)
        });
    }

    public async Task<IActionResult> Details(string id)
    {
        var e = await _employees.GetByIdAsync(id);
        if (e == null) return NotFound();
        return View(new EmployeeDetailsViewModel
        {
            Employee = await ToForm(e),
            DepartmentName = e.Department?.Name,
            PositionName = e.Position?.Name,
            IsActive = e.IsActive,
            UpcomingShifts = await _shifts.GetForPeriodAsync(DateTime.Now, DateTime.Today.AddDays(30), employeeId: id),
            Leaves = await _leaves.GetForEmployeeAsync(id),
            PaidLeaveDaysLeft = LeaveService.AnnualPaidLeaveDays - await _leaves.GetUsedPaidDaysAsync(id, DateTime.Today.Year)
        });
    }

    public async Task<IActionResult> Create() => View("Form", await FillLists(new EmployeeFormViewModel()));

    public async Task<IActionResult> Edit(string id)
    {
        var e = await _employees.GetByIdAsync(id);
        if (e == null) return NotFound();
        return View("Form", await FillLists(await ToForm(e)));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(EmployeeFormViewModel model)
    {
        bool isNew = string.IsNullOrEmpty(model.Id);
        if (isNew && string.IsNullOrEmpty(model.Password))
            ModelState.AddModelError(nameof(model.Password), "Въведете начална парола.");
        // Само администратор може да създава администратори и управители.
        if (model.Role != Roles.Employee && !User.IsInRole(Roles.Admin))
            ModelState.AddModelError(nameof(model.Role), "Само администратор може да дава тази роля.");
        if (!ModelState.IsValid) return View("Form", await FillLists(model));

        var entity = new Employee
        {
            Id = model.Id ?? Guid.NewGuid().ToString(),
            FirstName = model.FirstName.Trim(),
            LastName = model.LastName.Trim(),
            Email = model.Email.Trim(),
            PhoneNumber = model.PhoneNumber,
            DepartmentId = model.DepartmentId,
            PositionId = model.PositionId,
            HireDate = model.HireDate,
            WeeklyHours = model.WeeklyHours
        };

        ServiceResult result = isNew
            ? await _employees.CreateAsync(entity, model.Password!, model.Role)
            : await _employees.UpdateAsync(entity, model.Role);

        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("Form", await FillLists(model));
        }
        TempData["Success"] = isNew ? "Служителят е добавен." : "Промените са записани.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleActive(string id)
    {
        if (id == User.UserId())
        {
            TempData["Error"] = "Не можете да деактивирате собствения си акаунт.";
            return RedirectToAction(nameof(Index));
        }
        var e = await _employees.GetByIdAsync(id);
        if (e == null) return NotFound();
        await _employees.SetActiveAsync(id, !e.IsActive);
        TempData["Success"] = e.IsActive ? "Служителят е деактивиран." : "Служителят е активиран.";
        return RedirectToAction(nameof(Details), new { id });
    }

    private async Task<EmployeeFormViewModel> ToForm(Employee e) => new()
    {
        Id = e.Id, FirstName = e.FirstName, LastName = e.LastName, Email = e.Email ?? "",
        PhoneNumber = e.PhoneNumber, DepartmentId = e.DepartmentId, PositionId = e.PositionId,
        HireDate = e.HireDate, WeeklyHours = e.WeeklyHours,
        Role = await _employees.GetRoleAsync(e) ?? Roles.Employee
    };

    private async Task<EmployeeFormViewModel> FillLists(EmployeeFormViewModel model)
    {
        model.Departments = new SelectList(await _departments.GetAllAsync(), "Id", "Name", model.DepartmentId);
        model.Positions = new SelectList(await _positions.GetAllAsync(), "Id", "Name", model.PositionId);
        var roles = User.IsInRole(Roles.Admin) ? Roles.All : new[] { Roles.Employee };
        model.Roles = new SelectList(roles.Select(r => new { Value = r, Text = DisplayHelpers.RoleToBg(r) }), "Value", "Text", model.Role);
        return model;
    }
}
