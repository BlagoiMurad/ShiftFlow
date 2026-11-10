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

/// <summary>Отчитане на реално отработеното време.</summary>
[Authorize]
public class TimeEntriesController : Controller
{
    private readonly ITimeTrackingService _time;
    private readonly IShiftService _shifts;
    private readonly IEmployeeService _employees;
    private readonly IDepartmentService _departments;

    public TimeEntriesController(ITimeTrackingService time, IShiftService shifts, IEmployeeService employees, IDepartmentService departments)
    {
        _time = time;
        _shifts = shifts;
        _employees = employees;
        _departments = departments;
    }

    /// <summary>Моето време: бутон вход/изход и записи за избрания период.</summary>
    public async Task<IActionResult> Index(DateTime? from, DateTime? to)
    {
        var userId = User.UserId();
        var start = from ?? DbSeeder.WeekStart(DateTime.Today);
        var end = to ?? start.AddDays(6);
        var todayShifts = await _shifts.GetForPeriodAsync(DateTime.Now.AddHours(-1), DateTime.Now.AddHours(12), employeeId: userId);

        return View(new MyTimeViewModel
        {
            OpenEntry = await _time.GetOpenEntryAsync(userId),
            CurrentShift = todayShifts.FirstOrDefault(s => s.Status == ShiftStatus.Planned),
            From = start,
            To = end,
            Entries = await _time.GetEntriesAsync(start, end.AddDays(1), userId)
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ClockIn()
    {
        var result = await _time.ClockInAsync(User.UserId());
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? $"Вход отбелязан в {result.Value!.ClockIn:HH:mm}."
            : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ClockOut(string? note)
    {
        var result = await _time.ClockOutAsync(User.UserId(), note);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? $"Изход отбелязан. Отработени: {DisplayHelpers.Hours(result.Value!.WorkedHours)}"
            : result.Error;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Преглед на всички записи за управителя.</summary>
    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Manage(DateTime? from, DateTime? to, string? employeeId, int? departmentId)
    {
        var start = from ?? DbSeeder.WeekStart(DateTime.Today);
        var end = to ?? start.AddDays(6);
        return View(new TimeManageViewModel
        {
            From = start,
            To = end,
            EmployeeId = employeeId,
            DepartmentId = departmentId,
            Employees = new SelectList(await _employees.GetAllAsync(), "Id", "FullName", employeeId),
            Departments = new SelectList(await _departments.GetAllAsync(), "Id", "Name", departmentId),
            Entries = await _time.GetEntriesAsync(start, end.AddDays(1), employeeId, departmentId)
        });
    }

    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Create(string? employeeId) =>
        View("Form", await FillLists(new TimeEntryFormViewModel { EmployeeId = employeeId ?? string.Empty }));

    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Edit(int id)
    {
        var e = await _time.GetByIdAsync(id);
        if (e == null) return NotFound();
        return View("Form", await FillLists(new TimeEntryFormViewModel
        {
            Id = e.Id, EmployeeId = e.EmployeeId, ClockIn = e.ClockIn, ClockOut = e.ClockOut, ShiftId = e.ShiftId, Note = e.Note
        }));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Save(TimeEntryFormViewModel model)
    {
        if (string.IsNullOrWhiteSpace(model.Note))
            ModelState.AddModelError(nameof(model.Note), "Опишете причината за ръчната корекция.");
        if (!ModelState.IsValid) return View("Form", await FillLists(model));

        var result = await _time.SaveCorrectionAsync(new TimeEntry
        {
            Id = model.Id, EmployeeId = model.EmployeeId, ClockIn = model.ClockIn, ClockOut = model.ClockOut,
            ShiftId = model.ShiftId, Note = model.Note
        });
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("Form", await FillLists(model));
        }
        TempData["Success"] = "Записът е запазен.";
        return RedirectToAction(nameof(Manage), new { employeeId = model.EmployeeId, from = DbSeeder.WeekStart(model.ClockIn).ToString("yyyy-MM-dd") });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _time.DeleteAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Записът е изтрит." : result.Error;
        return RedirectToAction(nameof(Manage));
    }

    private async Task<TimeEntryFormViewModel> FillLists(TimeEntryFormViewModel model)
    {
        model.Employees = new SelectList(await _employees.GetAllAsync(onlyActive: true), "Id", "FullName", model.EmployeeId);
        var shifts = string.IsNullOrEmpty(model.EmployeeId)
            ? new List<Shift>()
            : await _shifts.GetForPeriodAsync(model.ClockIn.Date.AddDays(-7), model.ClockIn.Date.AddDays(7), employeeId: model.EmployeeId);
        model.Shifts = new SelectList(shifts.Select(s => new { s.Id, Text = $"{s.Start:dd.MM HH:mm}–{s.End:HH:mm}" }), "Id", "Text", model.ShiftId);
        return model;
    }
}
