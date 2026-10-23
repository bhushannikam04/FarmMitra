using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.Services;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FarmMitra.Areas.Farmer.Controllers;

[Area("Farmer")]
[Authorize(Roles = "Farmer")]
public class ProfileController : Controller
{
    private const string Role = "Farmer";
    private readonly UserManager<ApplicationUser> _users;
    private readonly SignInManager<ApplicationUser> _signIn;
    private readonly IWebHostEnvironment _env;

    public ProfileController(UserManager<ApplicationUser> users, SignInManager<ApplicationUser> signIn, IWebHostEnvironment env)
    {
        _users = users;
        _signIn = signIn;
        _env = env;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account", new { area = "" });
        return View(AccountSettings.ToModel(user, Role));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(ProfileViewModel model)
    {
        var user = await _users.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

        AccountSettings.Validate(model, Role, ModelState);
        var photoError = ImageUpload.Validate(model.Photo);
        if (photoError != null) ModelState.AddModelError(nameof(model.Photo), photoError);
        if (!ModelState.IsValid)
            return View("Index", AccountSettings.Fill(model, user, Role));

        AccountSettings.Apply(user, model, Role);
        await AccountSettings.ApplyPhotoAsync(user, model, _env.WebRootPath);
        var result = await _users.UpdateAsync(user);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors) ModelState.AddModelError("", e.Description);
            return View("Index", AccountSettings.Fill(model, user, Role));
        }

        TempData["Success"] = "Your profile was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public IActionResult Security() => View(new ChangePasswordViewModel());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View("Security", new ChangePasswordViewModel());

        var user = await _users.GetUserAsync(User);
        if (user == null) return RedirectToAction("Login", "Account", new { area = "" });

        var result = await _users.ChangePasswordAsync(user, model.CurrentPassword, model.NewPassword);
        if (!result.Succeeded)
        {
            foreach (var e in result.Errors)
                ModelState.AddModelError("", e.Code == "PasswordMismatch" ? "Your current password is incorrect." : e.Description);
            return View("Security", new ChangePasswordViewModel());
        }

        // Keep the user signed in with the new security stamp.
        await _signIn.RefreshSignInAsync(user);
        TempData["Success"] = "Your password was changed.";
        return RedirectToAction(nameof(Security));
    }
}
