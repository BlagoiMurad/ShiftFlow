using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShiftFlow.Data.Entities;
using ShiftFlow.Data.Enums;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Interfaces;
using ShiftFlow.Web.Data;
using ShiftFlow.Web.Helpers;
using ShiftFlow.Web.Models;

namespace ShiftFlow.Web.Controllers;

[Authorize]
public class ShiftsController : Controller
{
    private readonly IShiftService _shifts;
    private readonly IEmployeeService _employees;
    private readonly IDepartmentService _departments;
    private readonly ILeaveService _leaves;

    public ShiftsController(IShiftService shifts, IEmployeeService employees, IDepartmentService departments, ILeaveService leaves)
    {
        _shifts = shifts;
        _employees = employees;
        _departments = departments;
        _leaves = leaves;
    }

    /// <summary>Седмичен график. Всички служители виждат графика, управителите могат да го редактират.</summary>
    public async Task<IActionResult> Index(DateTime? week, int? departmentId)
    {
        var start = DbSeeder.WeekStart(week ?? DateTime.Today);
        var end = start.AddDays(7);

        // Служителят вижда по подразбиране своя отдел.
        if (!departmentId.HasValue && !User.IsManager())
            departmentId = (await _employees.GetByIdAsync(User.UserId()))?.DepartmentId;

        var leaves = (await _leaves.GetAllAsync(RequestStatus.Approved))
            .Where(l => l.StartDate < end && l.EndDate >= start).ToList();

        return View(new WeekScheduleViewModel
        {
            WeekStart = start,
            DepartmentId = departmentId,
            Departments = new SelectList(await _departments.GetAllAsync(), "Id", "Name", departmentId),
            // Служители без отдел (напр. администратор) не участват в графика.
            Employees = (await _employees.GetAllAsync(departmentId, onlyActive: true)).Where(e => e.DepartmentId != null).ToList(),
            Shifts = await _shifts.GetForPeriodAsync(start, end, departmentId),
            Leaves = leaves,
            CanEdit = User.IsManager()
        });
    }

    /// <summary>Моите смени.</summary>
    public async Task<IActionResult> My()
    {
        var userId = User.UserId();
        var all = await _shifts.GetForPeriodAsync(DateTime.Today.AddDays(-30), DateTime.Today.AddDays(60), employeeId: userId);
        return View(new MyShiftsViewModel
        {
            Upcoming = all.Where(s => s.End >= DateTime.Now).ToList(),
            Past = all.Where(s => s.End < DateTime.Now).OrderByDescending(s => s.Start).ToList()
        });
    }

    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Create(DateTime? date, string? employeeId, int? departmentId)
    {
        var model = new ShiftFormViewModel
        {
            Date = date ?? DateTime.Today,
            EmployeeId = employeeId ?? string.Empty,
        };
        if (departmentId.HasValue) model.DepartmentId = departmentId.Value;
        else if (employeeId != null)
            model.DepartmentId = (await _employees.GetByIdAsync(employeeId))?.DepartmentId ?? 0;
        return View("Form", await FillLists(model));
    }

    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Edit(int id)
    {
        var s = await _shifts.GetByIdAsync(id);
        if (s == null) return NotFound();
        return View("Form", await FillLists(new ShiftFormViewModel
        {
            Id = s.Id, EmployeeId = s.EmployeeId, DepartmentId = s.DepartmentId,
            Date = s.Start.Date, StartTime = s.Start.TimeOfDay, EndTime = s.End.TimeOfDay,
            BreakMinutes = s.BreakMinutes, Notes = s.Notes, Status = s.Status
        }));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Save(ShiftFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", await FillLists(model));

        var shift = new Shift
        {
            Id = model.Id,
            EmployeeId = model.EmployeeId,
            DepartmentId = model.DepartmentId,
            Start = model.StartDateTime,
            End = model.EndDateTime,
            BreakMinutes = model.BreakMinutes,
            Notes = model.Notes,
            Status = model.Status
        };
        var result = model.Id == 0 ? await _shifts.CreateAsync(shift) : await _shifts.UpdateAsync(shift);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("Form", await FillLists(model));
        }
        TempData["Success"] = "Смяната е записана.";
        return RedirectToAction(nameof(Index), new { week = model.Date.ToString("yyyy-MM-dd"), departmentId = model.DepartmentId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Delete(int id)
    {
        var shift = await _shifts.GetByIdAsync(id);
        var result = await _shifts.DeleteAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Смяната е изтрита." : result.Error;
        return RedirectToAction(nameof(Index), new { week = shift?.Start.ToString("yyyy-MM-dd"), departmentId = shift?.DepartmentId });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> CopyWeek(DateTime week, int? departmentId)
    {
        var result = await _shifts.CopyWeekAsync(week, departmentId);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? $"Копирани са {result.Value} смени в следващата седмица."
            : result.Error;
        return RedirectToAction(nameof(Index), new { week = week.AddDays(7).ToString("yyyy-MM-dd"), departmentId });
    }

    private async Task<ShiftFormViewModel> FillLists(ShiftFormViewModel model)
    {
        var employees = await _employees.GetAllAsync(onlyActive: true);
        model.Employees = new SelectList(employees.Select(e => new
        {
            e.Id,
            Name = e.FullName + (e.Department != null ? $" ({e.Department.Name})" : "")
        }), "Id", "Name", model.EmployeeId);
        model.Departments = new SelectList(await _departments.GetAllAsync(), "Id", "Name", model.DepartmentId);
        return model;
    }
}
