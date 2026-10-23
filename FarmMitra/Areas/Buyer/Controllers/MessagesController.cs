using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Areas.Buyer.Controllers;

public class MessagesController : BuyerBaseController
{
    public MessagesController(ApplicationDbContext db, UserManager<ApplicationUser> users) : base(db, users) { }

    public async Task<IActionResult> Index() =>
        View(new InboxViewModel { Area = "Buyer", Rows = await ChatHelper.GetInboxAsync(Db, CurrentUser.Id) });

    // "Contact Farmer" on a product page.
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Start(int productId)
    {
        var product = await Db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null) return NotFound();

        var conversation = await ChatHelper.FindOrCreateAsync(Db, CurrentUser.Id, product.FarmerId, product.Id);
        return RedirectToAction(nameof(Thread), new { id = conversation.Id });
    }

    public async Task<IActionResult> Thread(int id)
    {
        var vm = await ChatHelper.GetThreadAsync(Db, id, CurrentUser.Id, "Buyer");
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
