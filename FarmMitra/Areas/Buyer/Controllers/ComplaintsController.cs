using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Areas.Buyer.Controllers;

public class ComplaintsController : BuyerBaseController
{
    private const int PageSize = 8;
    private readonly IWebHostEnvironment _env;

    public ComplaintsController(ApplicationDbContext db, UserManager<ApplicationUser> users, IWebHostEnvironment env)
        : base(db, users) => _env = env;

    [HttpGet]
    public async Task<IActionResult> Index(string? q, string? status, int? orderId, int page = 1)
    {
        var vm = await BuildPageAsync(new ComplaintFormViewModel { OrderId = orderId }, q, status, page);
        return View(vm);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ComplaintFormViewModel form)
    {
        var uid = CurrentUser.Id;

        if (!ComplaintTypes.All.Contains(form.Type))
            ModelState.AddModelError(nameof(form.Type), "Choose a complaint type.");

        // A complaint can only be about one of the buyer's own orders.
        if (form.OrderId.HasValue && !await Db.Orders.AnyAsync(o => o.Id == form.OrderId && o.BuyerId == uid))
            ModelState.AddModelError(nameof(form.OrderId), "Choose one of your own orders.");

        var imageError = ImageUpload.Validate(form.Attachment);
        if (imageError != null)
            ModelState.AddModelError(nameof(form.Attachment), imageError);

        if (!ModelState.IsValid)
            return View("Index", await BuildPageAsync(form, null, null, 1));

        var complaint = new Complaint
        {
            UserId = uid,
            Type = form.Type,
            OrderId = form.OrderId,
            Subject = form.Subject.Trim(),
            Description = form.Description.Trim(),
            Status = "Pending"
        };

        if (form.Attachment is { Length: > 0 })
            complaint.AttachmentPath = await ImageUpload.SaveAsync(form.Attachment, _env.WebRootPath, "complaints");

        Db.Complaints.Add(complaint);
        await Db.SaveChangesAsync();

        TempData["Success"] = $"Complaint #CMP-{complaint.Id} submitted. We'll review it shortly.";
        return RedirectToAction(nameof(Index));
    }

    private async Task<ComplaintsPageViewModel> BuildPageAsync(ComplaintFormViewModel form, string? q, string? status, int page)
    {
        var uid = CurrentUser.Id;

        var orders = await Db.Orders
            .Where(o => o.BuyerId == uid)
            .OrderByDescending(o => o.OrderDate)
            .Select(o => new { o.Id, o.OrderDate, o.Total })
            .ToListAsync();

        var complaints = Db.Complaints.Where(c => c.UserId == uid);

        if (!string.IsNullOrWhiteSpace(status))
            complaints = complaints.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(q))
            complaints = complaints.Where(c => c.Subject.Contains(q) || c.Type.Contains(q));

        var rows = complaints.OrderByDescending(c => c.CreatedAt).Select(c => new ComplaintRow
        {
            Id = c.Id,
            UserName = c.User!.FullName,
            Type = c.Type,
            Subject = c.Subject,
            Description = c.Description,
            OrderId = c.OrderId,
            CreatedAt = c.CreatedAt,
            Status = c.Status
        });

        return new ComplaintsPageViewModel
        {
            Form = form,
            Orders = orders.Select(o => new OrderOption
            {
                Id = o.Id,
                Label = $"#FM-{o.Id} - {o.OrderDate:dd MMM yyyy} - ₹{o.Total:N0}"
            }).ToList(),
            List = await PagedResult<ComplaintRow>.CreateAsync(rows, page, PageSize, q, status)
        };
    }
}
