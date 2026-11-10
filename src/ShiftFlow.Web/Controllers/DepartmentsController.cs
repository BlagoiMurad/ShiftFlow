using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ShiftFlow.Data.Entities;
using ShiftFlow.Services.Common;
using ShiftFlow.Services.Interfaces;
using ShiftFlow.Web.Models;

namespace ShiftFlow.Web.Controllers;

[Authorize(Roles = Roles.AdminOrManager)]
public class DepartmentsController : Controller
{
    private readonly IDepartmentService _departments;
    public DepartmentsController(IDepartmentService departments) => _departments = departments;

    public async Task<IActionResult> Index() => View(await _departments.GetAllAsync());

    [Authorize(Roles = Roles.Admin)]
    public IActionResult Create() => View("Form", new DepartmentFormViewModel());

    [Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Edit(int id)
    {
        var d = await _departments.GetByIdAsync(id);
        if (d == null) return NotFound();
        return View("Form", new DepartmentFormViewModel
        {
            Id = d.Id, Name = d.Name, Description = d.Description, MinStaffPerShift = d.MinStaffPerShift
        });
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Save(DepartmentFormViewModel model)
    {
        if (!ModelState.IsValid) return View("Form", model);

        var entity = new Department
        {
            Id = model.Id, Name = model.Name.Trim(), Description = model.Description, MinStaffPerShift = model.MinStaffPerShift
        };
        var result = model.Id == 0 ? await _departments.CreateAsync(entity) : await _departments.UpdateAsync(entity);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, result.Error!);
            return View("Form", model);
        }
        TempData["Success"] = "Отделът е записан.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken, Authorize(Roles = Roles.Admin)]
    public async Task<IActionResult> Delete(int id)
    {
        var result = await _departments.DeleteAsync(id);
        TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded ? "Отделът е изтрит." : result.Error;
        return RedirectToAction(nameof(Index));
    }
}
