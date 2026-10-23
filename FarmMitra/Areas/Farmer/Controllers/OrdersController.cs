using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Areas.Farmer.Controllers;

public class OrdersController : FarmerBaseController
{
    private const int PageSize = 10;

    public OrdersController(ApplicationDbContext db, UserManager<ApplicationUser> users) : base(db, users) { }

    public async Task<IActionResult> Index(string? q, string? status, int page = 1)
    {
        var items = MyOrderItems();

        if (!string.IsNullOrWhiteSpace(status))
            items = items.Where(i => i.Order!.Status == status);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.TrimStart('#');
            items = items.Where(i => i.Product!.Name.Contains(term)
                                  || i.Order!.Buyer!.FullName.Contains(term)
                                  || i.OrderId.ToString().Contains(term));
        }

        var rows = ToOrderRows(items.OrderByDescending(i => i.Order!.OrderDate));
        return View(await PagedResult<FarmerOrderRow>.CreateAsync(rows, page, PageSize, q, status));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? returnUrl)
    {
        var uid = CurrentUser.Id;
        var order = await Db.Orders
            .Include(o => o.Items).ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (order == null || !order.Items.Any(i => i.Product!.FarmerId == uid))
            return NotFound();

        // An order that also contains other farmers' products can't be changed by one farmer.
        if (order.Items.Any(i => i.Product!.FarmerId != uid))
        {
            TempData["Error"] = "This order includes products from other farmers, so only an admin can change its status.";
            return Back(returnUrl, nameof(Index));
        }

        if (!OrderFlow.Next(order.Status).Contains(status))
        {
            TempData["Error"] = $"An order that is {order.Status.ToLower()} can't be moved to {status.ToLower()}.";
            return Back(returnUrl, nameof(Index));
        }

        if (status == "Cancelled")
        {
            await OrderStock.CancelAsync(Db, order);   // also puts the stock back
        }
        else
        {
            order.Status = status;
            await Db.SaveChangesAsync();
        }

        TempData["Success"] = $"Order #FM-{order.Id} marked as {status.ToLower()}.";
        return Back(returnUrl, nameof(Index));
    }
}
