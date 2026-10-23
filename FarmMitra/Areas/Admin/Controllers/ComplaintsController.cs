using FarmMitra.Data;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FarmMitra.Areas.Admin.Controllers;

public class ComplaintsController : AdminBaseController
{
    private const int PageSize = 10;
    private static readonly string[] ValidStatuses = { "Pending", "Under Review", "Resolved", "Rejected" };

    public ComplaintsController(ApplicationDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? q, string? status, int page = 1)
    {
        var complaints = Db.Complaints.AsQueryable();

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

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? returnUrl)
    {
        if (!ValidStatuses.Contains(status)) return BadRequest();

        var complaint = await Db.Complaints.FindAsync(id);
        if (complaint == null)
        {
            TempData["Error"] = "Complaint not found.";
            return Back(returnUrl, nameof(Index));
        }

        complaint.Status = status;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"Complaint #{complaint.Id} marked as {status.ToLower()}.";
        return Back(returnUrl, nameof(Index));
    }
}
