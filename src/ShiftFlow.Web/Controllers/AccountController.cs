using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using ShiftFlow.Data.Entities;
using ShiftFlow.Web.Models;

namespace ShiftFlow.Web.Controllers;

public class AccountController : Controller
{
    private readonly SignInManager<Employee> _signIn;
    private readonly UserManager<Employee> _users;

    public AccountController(SignInManager<Employee> signIn, UserManager<Employee> users)
    {
        _signIn = signIn;
        _users = users;
    }

    [AllowAnonymous]
    public IActionResult Login(string? returnUrl = null) => View(new LoginViewModel { ReturnUrl = returnUrl });

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _users.FindByEmailAsync(model.Email);
        if (user != null && !user.IsActive)
        {
            ModelState.AddModelError(string.Empty, "Акаунтът е деактивиран. Свържете се с управител.");
            return View(model);
        }

        var result = await _signIn.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, lockoutOnFailure: true);
        if (result.Succeeded)
            return Url.IsLocalUrl(model.ReturnUrl) ? Redirect(model.ReturnUrl!) : RedirectToAction("Index", "Home");

        ModelState.AddModelError(string.Empty, result.IsLockedOut
            ? "Акаунтът е временно заключен след няколко грешни опита."
            : "Грешен имейл или парола.");
        return View(model);
    }

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signIn.SignOutAsync();
        return RedirectToAction(nameof(Login));
    }

    [Authorize]
    public IActionResult ChangePassword() => View(new ChangePasswordViewModel());

    [HttpPost, Authorize, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);
        var user = await _users.GetUserAsync(User);
        if (user == null) return RedirectToAction(nameof(Login));

        var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            ModelState.AddModelError(string.Empty, "Текущата парола е грешна или новата не отговаря на изискванията.");
            return View(model);
        }
        await _signIn.RefreshSignInAsync(user);
        TempData["Success"] = "Паролата е сменена.";
        return RedirectToAction("Index", "Home");
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();
}
