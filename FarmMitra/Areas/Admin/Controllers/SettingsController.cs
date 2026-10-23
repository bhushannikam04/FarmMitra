using FarmMitra.Data;
using FarmMitra.Services;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FarmMitra.Areas.Admin.Controllers;

public class SettingsController : AdminBaseController
{
    private readonly SettingsService _settings;

    public SettingsController(ApplicationDbContext db, SettingsService settings) : base(db) => _settings = settings;

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var s = await _settings.GetAsync();
        return View(new PlatformSettingsViewModel
        {
            FlatDeliveryFee = s.FlatDeliveryFee,
            FreeDeliveryAbove = s.FreeDeliveryAbove,
            SupportEmail = s.SupportEmail,
            SupportPhone = s.SupportPhone
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(PlatformSettingsViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        await _settings.UpdateAsync(new SiteSettings(
            model.FlatDeliveryFee, model.FreeDeliveryAbove, model.SupportEmail, model.SupportPhone));

        TempData["Success"] = "Platform settings saved. They apply to new carts straight away.";
        return RedirectToAction(nameof(Index));
    }
}
