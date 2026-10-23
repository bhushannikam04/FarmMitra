using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FarmMitra.Areas.Farmer.Controllers;

// Buyers start conversations; farmers reply, report and read them here.
public class MessagesController : FarmerBaseController
{
    public MessagesController(ApplicationDbContext db, UserManager<ApplicationUser> users) : base(db, users) { }

    public async Task<IActionResult> Index() =>
        View(new InboxViewModel { Area = "Farmer", Rows = await ChatHelper.GetInboxAsync(Db, CurrentUser.Id) });

    public async Task<IActionResult> Thread(int id)
    {
        var vm = await ChatHelper.GetThreadAsync(Db, id, CurrentUser.Id, "Farmer");
        return vm == null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Send(int id, string? body)
    {
        var error = await ChatHelper.SendAsync(Db, id, CurrentUser.Id, body);
        if (error != null) TempData["Error"] = error;
        return RedirectToAction(nameof(Thread), new { id });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Report(int id, string? reason)
    {
        if (!await ChatHelper.ReportAsync(Db, id, CurrentUser.Id, reason)) return NotFound();
        TempData["Success"] = "Thanks. An administrator will review this conversation.";
        return RedirectToAction(nameof(Thread), new { id });
    }
}
