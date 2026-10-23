using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FarmMitra.Controllers;

public class AccountController : Controller
{
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly SignInManager<ApplicationUser> _signInManager;

    public AccountController(UserManager<ApplicationUser> userManager, SignInManager<ApplicationUser> signInManager)
    {
        _userManager = userManager;
        _signInManager = signInManager;
    }

    // ---------- LOGIN ----------
    [HttpGet, AllowAnonymous]
    public IActionResult Login(string? returnUrl = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new LoginViewModel { ReturnUrl = returnUrl });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var user = await _userManager.FindByEmailAsync(model.Email)
                   ?? await _userManager.FindByNameAsync(model.Email);

        if (user == null)
        {
            ModelState.AddModelError("", "Invalid email or password.");
            return View(model);
        }

        var result = await _signInManager.PasswordSignInAsync(user, model.Password, model.RememberMe, lockoutOnFailure: true);

        if (result.Succeeded)
        {
            // Farmers must be approved by an admin before they can use the system.
            if (!user.IsApproved)
            {
                await _signInManager.SignOutAsync();
                return RedirectToAction(nameof(PendingApproval), new { rejected = user.ApprovalStatus == "Rejected" });
            }

            if (!string.IsNullOrEmpty(model.ReturnUrl) && Url.IsLocalUrl(model.ReturnUrl))
                return LocalRedirect(model.ReturnUrl);

            return Redirect(await GetLandingUrlAsync(user));
        }

        ModelState.AddModelError("", result.IsLockedOut
            ? "Too many failed attempts. Please try again in a few minutes."
            : "Invalid email or password.");
        return View(model);
    }

    // ---------- REGISTER ----------
    [HttpGet, AllowAnonymous]
    public IActionResult Register(string? role = null)
    {
        if (User.Identity?.IsAuthenticated == true) return RedirectToAction("Index", "Home");
        return View(new RegisterViewModel { Role = role == "Buyer" ? "Buyer" : "Farmer" });
    }

    [HttpPost, AllowAnonymous, ValidateAntiForgeryToken]
    public async Task<IActionResult> Register(RegisterViewModel model)
    {
        // Never trust the posted role: only these two can be self-registered ("Admin" never).
        if (model.Role != "Farmer" && model.Role != "Buyer")
            ModelState.AddModelError(nameof(model.Role), "Please choose Farmer or Buyer.");

        if (model.Role == "Farmer")
        {
            if (string.IsNullOrWhiteSpace(model.FarmLocation))
                ModelState.AddModelError(nameof(model.FarmLocation), "Farm location is required for farmers.");
            if (string.IsNullOrWhiteSpace(model.CropsGrown))
                ModelState.AddModelError(nameof(model.CropsGrown), "Tell us what you grow.");
        }

        if (!ModelState.IsValid) return View(model);

        var user = new ApplicationUser
        {
            UserName = model.Email,
            Email = model.Email,
            EmailConfirmed = true,               // no email service yet
            PhoneNumber = model.PhoneNumber,
            FullName = model.FullName,
            Address = model.Address,
            City = model.City,
            State = model.State,
            IsApproved = model.Role == "Buyer",  // farmers wait for admin approval
            ApprovalStatus = model.Role == "Buyer" ? "Approved" : "Pending",
            FarmLocation = model.Role == "Farmer" ? model.FarmLocation : null,
            FarmSizeAcres = model.Role == "Farmer" ? model.FarmSizeAcres : null,
            CropsGrown = model.Role == "Farmer" ? model.CropsGrown : null,
            ExperienceYears = model.Role == "Farmer" ? model.ExperienceYears : null
        };

        var result = await _userManager.CreateAsync(user, model.Password);
        if (!result.Succeeded)
        {
            foreach (var error in result.Errors)
                ModelState.AddModelError("", error.Description);
            return View(model);
        }

        await _userManager.AddToRoleAsync(user, model.Role);

        if (model.Role == "Farmer")
            return RedirectToAction(nameof(PendingApproval));

        await _signInManager.SignInAsync(user, isPersistent: false);
        return Redirect(await GetLandingUrlAsync(user));
    }

    // ---------- LOGOUT ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await _signInManager.SignOutAsync();
        return RedirectToAction("Index", "Home");
    }

    // ---------- INFO PAGES ----------
    [AllowAnonymous]
    public IActionResult PendingApproval(bool rejected = false)
    {
        ViewBag.Rejected = rejected;
        return View();
    }

    [AllowAnonymous]
    public IActionResult AccessDenied() => View();

    // ---------- HELPERS ----------
    // Where to send a user right after signing in.
    // STEP 6 (dashboards): switch this to return "/Admin/Dashboard", "/Farmer/Dashboard", "/Buyer/Dashboard".
    private async Task<string> GetLandingUrlAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        if (roles.Contains("Admin"))  return "/Admin/Dashboard";
        if (roles.Contains("Farmer")) return "/Farmer/Dashboard";
        return "/Buyer/Dashboard";
    }
}
