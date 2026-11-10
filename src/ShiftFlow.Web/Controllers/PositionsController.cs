using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShiftFlow.Data.Entities;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Interfaces;
using ShiftFlow.Web.Models;

namespace ShiftFlow.Web.Controllers;

[Authorize(Roles = Roles.Admin)]
public class PositionsController : Controller
{
    private readonly IPositionService _positions;
    public PositionsController(IPositionService positions) => _positions = positions;

    public async Task<IActionResult> Index() => View(await _positions.GetAllAsync());

    public IActionResult Create() => View("Form", new PositionFormViewModel());

    public async Task<IActionResult> Edit(int id)
    {
        var p = await _positions.GetByIdAsync(id);
        if (p == null) return NotFound();
        return View("Form", new PositionFormViewModel { Id = p.Id, Name = p.Name, HourlyRate = p.HourlyRate });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Save(PositionFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);
        var entity = new Position { Id = model.Id, Name = model.Name.Trim(), HourlyRate = model.HourlyRate };
        var result = model.Id == 0 ? await _positions.CreateAsync(entity) : await _positions.UpdateAsync(entity);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("Form", model);
        }
        TempData["Success"] = "Длъжността е записана.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _positions.DeleteAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Длъжността е изтрита." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
