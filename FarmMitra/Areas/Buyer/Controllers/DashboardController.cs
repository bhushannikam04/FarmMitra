using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Areas.Buyer.Controllers;

public class DashboardController : BuyerBaseController
{
    public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> users) : base(db, users) { }

    public async Task<IActionResult> Index()
    {
        var uid = CurrentUser.Id;
        var myOrders = Db.Orders.Where(o => o.BuyerId == uid);
        var active = new[] { "Pending", "Confirmed", "Shipped" };

        var boughtCategories = await Db.OrderItems
            .Where(i => i.Order!.BuyerId == uid)
            .Select(i => i.Product!.Category)
            .Distinct()
            .ToListAsync();

        // Newest available products, with categories this buyer has bought before shown first.
        var candidates = await Db.Products.Include(p => p.Farmer)
            .Where(p => p.Status == "Available" && p.Quantity > 0)
            .OrderByDescending(p => p.CreatedAt)
            .Take(12)
            .ToListAsync();

        var vm = new BuyerDashboardViewModel
        {
            FirstName = CurrentUser.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "there",
            CartItems = await Db.CartItems.CountAsync(c => c.BuyerId == uid),
            ActiveOrders = await myOrders.CountAsync(o => active.Contains(o.Status)),
            DeliveredOrders = await myOrders.CountAsync(o => o.Status == "Delivered"),
            TotalPurchases = await myOrders.Where(o => o.Status != "Cancelled").SumAsync(o => (decimal?)o.Total) ?? 0,
            ActiveOrderRows = await ToOrderRows(myOrders.Where(o => active.Contains(o.Status)).OrderByDescending(o => o.OrderDate).Take(5)).ToListAsync(),
            Recommended = candidates.OrderByDescending(p => boughtCategories.Contains(p.Category)).Take(6).ToList()
        };

        return View(vm);
    }
}
