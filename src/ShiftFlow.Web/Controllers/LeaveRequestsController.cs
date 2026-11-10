using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShiftFlow.Data.Entities;
using ShiftFlow.Data.Enums;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Implementations;
using ShiftFlow.Services.Interfaces;
using ShiftFlow.Web.Helpers;
using ShiftFlow.Web.Models;

namespace ShiftFlow.Web.Controllers;

[Authorize]
public class LeaveRequestsController : Controller
{
    private readonly ILeaveService _leaves;
    public LeaveRequestsController(ILeaveService leaves) => _leaves = leaves;

    public async Task<IActionResult> Index()
    {
        var userId = User.UserId();
        int used = await _leaves.GetUsedPaidDaysAsync(userId, DateTime.Today.Year);
        return View(new LeaveIndexViewModel
        {
            Requests = await _leaves.GetForEmployeeAsync(userId),
            PaidDaysUsed = used,
            PaidDaysLeft = LeaveService.AnnualPaidLeaveDays - used
        });
    }

    public async Task<IActionResult> Create() => View(new LeaveRequestFormViewModel
    {
        PaidDaysLeft = LeaveService.AnnualPaidLeaveDays - await _leaves.GetUsedPaidDaysAsync(User.UserId(), DateTime.Today.Year)
    });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(LeaveRequestFormViewModel model)
    {
        if (ModelState.IsValid)
        {
            var result = await _leaves.CreateAsync(new LeaveRequest
            {
                EmployeeId = User.UserId(),
                Type = model.Type,
                StartDate = model.StartDate,
                EndDate = model.EndDate,
                Reason = model.Reason
            });
            if (result.Succeeded)
            {
                TempData["Success"] = "Заявката за отпуск е изпратена.";
                return RedirectToAction(nameof(Index));
            }
            ModelState.AddModelError(string.Empty, result.Error!);
        }
        model.PaidDaysLeft = LeaveService.AnnualPaidLeaveDays - await _leaves.GetUsedPaidDaysAsync(User.UserId(), DateTime.Today.Year);
        return View(model);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var result = await _leaves.CancelAsync(id, User.UserId());
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Заявката е оттеглена." : result.Error;
        return RedirectToAction(nameof(Index));
    }

    [Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Manage(RequestStatus? status = RequestStatus.PendingManager) =>
        View(new LeaveManageViewModel { Status = status, Requests = await _leaves.GetAllAsync(status) });

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.AdminOrManager)]
    public async Task<IActionResult> Review(ReviewViewModel model)
    {
        var result = await _leaves.ReviewAsync(model.Id, User.UserId(), model.Approve, model.Comment);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
            ? (model.Approve ? "Отпускът е одобрен. Смените в този период са отменени." : "Заявката е отхвърлена.")
            : result.Error;
        return RedirectToAction(nameof(Manage));
    }
}
