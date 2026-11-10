using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using ShiftFlow.Data.Enums;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Interfaces;
using ShiftFlow.Web.Helpers;
using ShiftFlow.Web.Models;

namespace ShiftFlow.Web.Controllers;

[Authorize]
public class SwapRequestsController : Controller
{
    private readonly ISwapRequestService _swaps;
    private readonly IShiftService _shifts;
    private readonly IEmployeeService _employees;

    public SwapRequestsController(ISwapRequestService swaps, IShiftService shifts, IEmployeeService employees)
    {
        _swaps = swaps;
        _shifts = shifts;
        _employees = employees;
    }

    /// <summary>Моите заявки и заявките към мен.</summary>
    public async Task<IActionResult> Index() => View(new SwapRequestIndexViewModel
    {
        CurrentUserId = User.UserId(),
        Requests = await _swaps.GetForEmployeeAsync(User.UserId())
    });

    public async Task<IActionResult> Create(int? shiftId) =>
        View(await FillLists(new SwapRequestFormViewModel { ShiftId = shiftId ?? 0 }));

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SwapRequestFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _swaps.CreateAsync(User.UserId(), model.ShiftId, model.TargetEmployeeId, model.TargetShiftId, model.Reason);
            if (result.Succeeded)
            {
                TempData["Success"] = "Заявката е изпратена на колегата.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        return View(await FillLists(model));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Respond(int id, bool accept)
    {
        var result = await _swaps.RespondAsColleagueAsync(id, User.UserId(), accept);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? (accept ? "Приехте размяната. Тя чака одобрение от управител." : "Отказахте размяната.")
            : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _swaps.CancelAsync(id, User.UserId());
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Заявката е оттеглена." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    /// <summary>Заявки, чакащи решение от управител.</summary>
    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Manage(bool all = false)
    {
        ViewBag.ShowAll = all;
        return View(all ? await _swaps.GetAllAsync() : await _swaps.GetPendingForManagerAsync());
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Review(ReviewViewModel model)
    {
        var result = await _swaps.ReviewAsync(model.Id, User.UserId(), model.Approve, model.Comment);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? (model.Approve ? "Размяната е одобрена и графикът е обновен." : "Заявката е отхвърлена.")
            : result.Error;
        return RedirectToAction(nameof(Manage));
    }

    private async Task<SwapRequestFormViewModel> FillLists(SwapRequestFormViewModel model)
    {
        var userId = User.UserId();
        var me = await _employees.GetByIdAsync(userId);
        var from = DateTime.Now;
        var to = DateTime.Today.AddDays(60);

        var myShifts = (await _shifts.GetForPeriodAsync(from, to, employeeId: userId))
            .Where(s => s.Status == ShiftStatus.Planned && s.Start > from);
        model.MyShifts = new SelectList(myShifts.Select(s => new
        {
            s.Id,
            Text = $"{s.Start:dd.MM (ddd)} {s.Start:HH:mm}–{s.End:HH:mm}, {s.Department.Name}"
        }), "Id", "Text", model.ShiftId);

        var colleagues = (await _employees.GetAllAsync(me?.DepartmentId, onlyActive: true)).Where(e => e.Id != userId);
        model.Colleagues = new SelectList(colleagues, "Id", "FullName", model.TargetEmployeeId);

        model.ColleagueShifts = (await _shifts.GetForPeriodAsync(from, to, me?.DepartmentId))
            .Where(s => s.EmployeeId != userId && s.Status == ShiftStatus.Planned && s.Start > from)
            .ToList();
        return model;
    }
}
