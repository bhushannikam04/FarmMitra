using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Areas.Buyer.Controllers;

public class OrdersController : BuyerBaseController
{
    private const int PageSize = 10;

    public OrdersController(ApplicationDbContext db, UserManager<ApplicationUser> users) : base(db, users) { }

    public async Task<IActionResult> Index(string? q, string? status, int page = 1)
    {
        var uid = CurrentUser.Id;
        var orders = Db.Orders.Where(o => o.BuyerId == uid);

        if (!string.IsNullOrWhiteSpace(status))
            orders = orders.Where(o => o.Status == status);

        if (!string.IsNullOrWhiteSpace(q))
        {
            var term = q.TrimStart('#');
            orders = orders.Where(o => o.Id.ToString().Contains(term)
                                    || o.Items.Any(i => i.Product!.Name.Contains(term)));
        }

        var rows = ToOrderRows(orders.OrderByDescending(o => o.OrderDate));
        return View(await PagedResult<OrderListRow>.CreateAsync(rows, page, PageSize, q, status));
    }

    public async Task<IActionResult> Details(int id)
    {
        var uid = CurrentUser.Id;

        var order = await Db.Orders
            .Where(o => o.Id == id && o.BuyerId == uid)
            .Select(o => new OrderDetailsViewModel
            {
                Id = o.Id,
                OrderDate = o.OrderDate,
                Status = o.Status,
                Total = o.Total,
                DeliveryFee = o.DeliveryFee,
                DeliveryAddress = o.DeliveryAddress,
                PaymentMethod = o.PaymentMethod,
                Lines = o.Items.Select(i => new OrderLineRow
                {
                    ProductId = i.ProductId,
                    Name = i.Product!.Name,
                    ImagePath = i.Product.ImagePath,
                    Unit = i.Product.Unit,
                    FarmerName = i.Product.Farmer!.FullName,
                    Quantity = i.Quantity,
                    UnitPrice = i.UnitPrice
                }).ToList()
            })
            .FirstOrDefaultAsync();

        return order == null ? NotFound() : View(order);
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Cancel(int id)
    {
        var uid = CurrentUser.Id;
        var order = await Db.Orders.FirstOrDefaultAsync(o => o.Id == id && o.BuyerId == uid);
        if (order == null) return NotFound();

        if (order.Status != "Pending")
        {
            TempData["Error"] = "Only orders that haven't been confirmed yet can be cancelled.";
            return RedirectToAction(nameof(Details), new { id });
        }

        await OrderStock.CancelAsync(Db, order);
        TempData["Success"] = $"Order #FM-{order.Id} was cancelled.";
        return RedirectToAction(nameof(Details), new { id });
    }
}
