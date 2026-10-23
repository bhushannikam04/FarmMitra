using FarmMitra.Data;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FarmMitra.Areas.Admin.Controllers;

public class BuyersController : AdminBaseController
{
    private const int PageSize = 10;

    public BuyersController(ApplicationDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? q, int page = 1)
    {
        var buyerIds = UserIdsInRole("Buyer");
        var users = Db.Users.Where(u => buyerIds.Contains(u.Id));

        if (!string.IsNullOrWhiteSpace(q))
            users = users.Where(u => u.FullName.Contains(q) || u.Email!.Contains(q)
                                  || (u.City ?? "").Contains(q) || (u.PhoneNumber ?? "").Contains(q));

        var rows = ToRows(users.OrderByDescending(u => u.CreatedAt));
        return View(await PagedResult<UserRow>.CreateAsync(rows, page, PageSize, q, null));
    }
}
