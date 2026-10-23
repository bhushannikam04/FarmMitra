using FarmMitra.Data;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Areas.Admin.Controllers;

// Admins can read every conversation and moderate it. They never send or mark messages as read.
public class MessagesController : AdminBaseController
{
    private const int PageSize = 10;

    public MessagesController(ApplicationDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? q, string? status, int page = 1)
    {
        var conversations = Db.Conversations.AsQueryable();

        conversations = status switch
        {
            "Reported" => conversations.Where(c => c.IsReported),
            "Closed"   => conversations.Where(c => c.IsClosed),
            "Active"   => conversations.Where(c => !c.IsClosed),
            _          => conversations
        };

        if (!string.IsNullOrWhiteSpace(q))
            conversations = conversations.Where(c =>
                c.Buyer!.FullName.Contains(q) ||
                c.Farmer!.FullName.Contains(q) ||
                (c.Product != null && c.Product.Name.Contains(q)) ||
                c.Messages.Any(m => m.Body.Contains(q)));

        var rows = conversations
            .OrderByDescending(c => c.IsReported)
            .ThenByDescending(c => c.LastMessageAt)
            .Select(c => new AdminConversationRow
            {
                Id = c.Id,
                BuyerName = c.Buyer!.FullName,
                FarmerName = c.Farmer!.FullName,
                ProductName = c.Product != null ? c.Product.Name : null,
                MessageCount = c.Messages.Count,
                LastMessageAt = c.LastMessageAt,
                IsReported = c.IsReported,
                ReportReason = c.ReportReason,
                IsClosed = c.IsClosed
            });

        return View(await PagedResult<AdminConversationRow>.CreateAsync(rows, page, PageSize, q, status));
    }

    public async Task<IActionResult> Thread(int id)
    {
        var vm = await ChatHelper.GetThreadAsync(Db, id, null, "Admin");
        return vm == null ? NotFound() : View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> ToggleHide(int id, string? returnUrl)
    {
        var message = await Db.Messages.FindAsync(id);
        if (message == null) return NotFound();

        message.IsHidden = !message.IsHidden;
        await Db.SaveChangesAsync();

        TempData["Success"] = message.IsHidden ? "Message hidden from both people." : "Message restored.";
        return Back(returnUrl, nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetClosed(int id, bool closed, string? returnUrl)
    {
        var conversation = await Db.Conversations.FindAsync(id);
        if (conversation == null) return NotFound();

        conversation.IsClosed = closed;
        await Db.SaveChangesAsync();

        TempData["Success"] = closed ? "Conversation closed. Nobody can send new messages." : "Conversation reopened.";
        return Back(returnUrl, nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> DismissReport(int id, string? returnUrl)
    {
        var conversation = await Db.Conversations.FindAsync(id);
        if (conversation == null) return NotFound();

        conversation.IsReported = false;
        conversation.ReportReason = null;
        await Db.SaveChangesAsync();

        TempData["Success"] = "Report dismissed.";
        return Back(returnUrl, nameof(Index));
    }
}
