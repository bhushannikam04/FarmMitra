using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Areas.Farmer.Controllers;

public class DashboardController : FarmerBaseController
{
    private const int LowStockLimit = 10;

    public DashboardController(ApplicationDbContext db, UserManager<ApplicationUser> users) : base(db, users) { }

    public async Task<IActionResult> Index()
    {
        var uid = CurrentUser.Id;
        var items = MyOrderItems();

        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
        var months = Enumerable.Range(0, 6).Select(i => start.AddMonths(i)).ToList();

        var sales = await items
            .Where(i => i.Order!.OrderDate >= start && i.Order.Status != "Cancelled")
            .Select(i => new { i.Order!.OrderDate, Amount = i.Quantity * i.UnitPrice })
            .ToListAsync();

        var vm = new FarmerDashboardViewModel
        {
            FirstName = CurrentUser.FullName.Split(' ', StringSplitOptions.RemoveEmptyEntries).FirstOrDefault() ?? "there",
            TotalProducts = await Db.Products.CountAsync(p => p.FarmerId == uid),
            PendingOrders = await items.Where(i => i.Order!.Status == "Pending").Select(i => i.OrderId).Distinct().CountAsync(),
            CompletedOrders = await items.Where(i => i.Order!.Status == "Delivered").Select(i => i.OrderId).Distinct().CountAsync(),
            TotalSales = await items.Where(i => i.Order!.Status != "Cancelled").SumAsync(i => (decimal?)(i.Quantity * i.UnitPrice)) ?? 0,

            SalesChart = months.Select(m => new MonthBar
            {
                Label = m.ToString("MMM"),
                Value = sales.Where(s => s.OrderDate.Year == m.Year && s.OrderDate.Month == m.Month).Sum(s => s.Amount)
            }).ToList(),

            LowStock = await Db.Products
                .Where(p => p.FarmerId == uid && p.Quantity <= LowStockLimit)
                .OrderBy(p => p.Quantity)
                .Take(5)
                .ToListAsync(),

            RecentOrders = await ToOrderRows(items.OrderByDescending(i => i.Order!.OrderDate).Take(5)).ToListAsync(),

            RecentProducts = await Db.Products
                .Where(p => p.FarmerId == uid)
                .OrderByDescending(p => p.CreatedAt)
                .Take(5)
                .ToListAsync()
        };

        return View(vm);
    }
}
