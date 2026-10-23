using FarmMitra.Data;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Areas.Admin.Controllers;

public class FarmersController : AdminBaseController
{
    private const int PageSize = 10;

    public FarmersController(ApplicationDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? q, string? status, int page = 1)
    {
        var farmerIds = UserIdsInRole("Farmer");
        var users = Db.Users.Where(u => farmerIds.Contains(u.Id));

        if (!string.IsNullOrWhiteSpace(status))
            users = users.Where(u => u.ApprovalStatus == status);

        if (!string.IsNullOrWhiteSpace(q))
            users = users.Where(u => u.FullName.Contains(q) || u.Email!.Contains(q)
                                  || (u.City ?? "").Contains(q) || (u.PhoneNumber ?? "").Contains(q));

        var rows = ToRows(users.OrderByDescending(u => u.CreatedAt));
        return View(await PagedResult<UserRow>.CreateAsync(rows, page, PageSize, q, status));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Approve(string id, string? returnUrl) => SetApproval(id, true, "Approved", returnUrl);

    [HttpPost, ValidateAntiForgeryToken]
    public Task<IActionResult> Reject(string id, string? returnUrl) => SetApproval(id, false, "Rejected", returnUrl);

    private async Task<IActionResult> SetApproval(string id, bool approved, string status, string? returnUrl)
    {
        // Only accounts in the Farmer role can be approved / rejected here.
        var isFarmer = await UserIdsInRole("Farmer").AnyAsync(x => x == id);
        var user = isFarmer ? await Db.Users.FirstOrDefaultAsync(u => u.Id == id) : null;

        if (user == null)
        {
            TempData["Error"] = "Farmer not found.";
            return Back(returnUrl, nameof(Index));
        }

        user.IsApproved = approved;
        user.ApprovalStatus = status;
        await Db.SaveChangesAsync();

        TempData["Success"] = $"{user.FullName} has been {status.ToLower()}.";
        return Back(returnUrl, nameof(Index));
    }
}
