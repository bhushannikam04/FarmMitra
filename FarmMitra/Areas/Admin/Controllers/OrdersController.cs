using FarmMitra.Data;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Mvc;

namespace FarmMitra.Areas.Admin.Controllers;

public class OrdersController : AdminBaseController
{
    private const int PageSize = 10;
    private static readonly string[] ValidStatuses = { "Pending", "Confirmed", "Shipped", "Delivered", "Cancelled" };

    public OrdersController(ApplicationDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? q, string? status, int page = 1)
    {
        var orders = Db.Orders.AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            orders = orders.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.TrimStart('#');
            orders = orders.Where(o => o.Buyer!.FullName.Contains(term) || o.Id.ToString().Contains(term));
        }

        var rows = orders.OrderByDescending(o => o.OrderDate).Select(o => new OrderRow
        {
            Id = o.Id,
            BuyerName = o.Buyer!.FullName,
            BuyerEmail = o.Buyer.Email,
            ItemCount = o.Items.Count,
            Total = o.Total,
            OrderDate = o.OrderDate,
            Status = o.Status
        });

        return View(await PagedResult<OrderRow>.CreateAsync(rows, page, PageSize, q, status));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> UpdateStatus(int id, string status, string? returnUrl)
    {
        if (!ValidStatuses.Contains(status)) return BadRequest();

        var order = await Db.Orders.FindAsync(id);
        if (order == null)
        {
            TempData["Error"] = "Order not found.";
            return Back(returnUrl, nameof(Index));
        }

        if (order.Status == "Cancelled")
        {
            TempData["Error"] = "Cancelled orders can't be changed.";
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

        TempData["Success"] = $"Order #{order.Id} marked as {status.ToLower()}.";
        return Back(returnUrl, nameof(Index));
    }
}
