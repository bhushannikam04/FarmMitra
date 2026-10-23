using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FarmMitra.Areas.Farmer.Controllers;

// Read-only: shows complaints buyers raised about orders containing this farmer's products.
public class ComplaintsController : FarmerBaseController
{
    private const int PageSize = 10;

    public ComplaintsController(ApplicationDbContext db, UserManager<ApplicationUser> users) : base(db, users) { }

    public async Task<IActionResult> Index(string? q, string? status, int page = 1)
    {
        var myOrderIds = MyOrderItems().Select(i => i.OrderId);
        var complaints = Db.Complaints.Where(c => c.OrderId != null && myOrderIds.Contains(c.OrderId.Value));

        if (!string.IsNullOrWhiteSpace(status))
            complaints = complaints.Where(c => c.Status == status);

        if (!string.IsNullOrWhiteSpace(q))
            complaints = complaints.Where(c => c.Subject.Contains(q) || c.Type.Contains(q) || c.User!.FullName.Contains(q));

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

        return View(await PagedResult<ComplaintRow>.CreateAsync(rows, page, PageSize, q, status));
    }
}
