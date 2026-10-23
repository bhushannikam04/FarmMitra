using System.Diagnostics;
using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _db;

    public HomeController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index()
    {
        var featured = await _db.Products
            .Include(p => p.Farmer)
            .Where(p => p.Status == "Available" && p.Quantity > 0)
            .OrderByDescending(p => p.CreatedAt)
            .Take(8)
            .ToListAsync();

        ViewData["Farmers"] = await LoadFarmersAsync();
        ViewData["Ticker"] = await LoadTickerAsync();
        ViewData["Stats"] = await LoadStatsAsync();
        return View(featured);
    }

    /// <summary>Just the farmer section. The homepage fetches this every 30 seconds to stay live.</summary>
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public async Task<IActionResult> Farmers() =>
        PartialView("_FarmerSpotlight", await LoadFarmersAsync());

    public IActionResult Privacy() => View();

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error() =>
        View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });

    // ---------------------------------------------------------------- helpers
    /// <summary>The newest available products, used for the scrolling price strip and the savings calculator.</summary>
    private async Task<List<TickerItem>> LoadTickerAsync() =>
        await _db.Products.AsNoTracking()
            .Where(p => p.Status == "Available" && p.Quantity > 0)
            .OrderByDescending(p => p.CreatedAt)
            .Take(16)
            .Select(p => new TickerItem { Name = p.Name, Price = p.Price, Unit = p.Unit, Location = p.Location })
            .ToListAsync();

    /// <summary>Real counts from the database: approved farmers, live listings, delivered orders and the buyers behind them.</summary>
    private async Task<HomeStats> LoadStatsAsync()
    {
        var farmerIds =
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where r.Name == "Farmer"
            select ur.UserId;

        var delivered = await _db.OrderItems.AsNoTracking()
            .Where(i => i.Order!.Status == "Delivered")
            .Select(i => new { i.OrderId, i.Order!.BuyerId })
            .Distinct()
            .ToListAsync();

        return new HomeStats
        {
            Farmers = await _db.Users.CountAsync(u => farmerIds.Contains(u.Id) && u.ApprovalStatus == "Approved"),
            Products = await _db.Products.CountAsync(p => p.Status == "Available" && p.Quantity > 0),
            DeliveredOrders = delivered.Count,
            Buyers = delivered.Select(d => d.BuyerId).Distinct().Count()
        };
    }

    private async Task<List<FarmerSpotlight>> LoadFarmersAsync()
    {
        var farmerIds =
            from ur in _db.UserRoles
            join r in _db.Roles on ur.RoleId equals r.Id
            where r.Name == "Farmer"
            select ur.UserId;

        var farmers = await _db.Users.AsNoTracking()
            .Where(u => farmerIds.Contains(u.Id) && u.ApprovalStatus == "Approved")
            .Select(u => new { u.Id, u.FullName, u.ProfileImagePath, u.City, u.State, u.CropsGrown, u.ExperienceYears, u.CreatedAt })
            .ToListAsync();

        if (farmers.Count == 0) return new List<FarmerSpotlight>();

        var ids = farmers.Select(f => f.Id).ToList();

        var productCounts = await _db.Products
            .Where(p => p.Status == "Available" && p.Quantity > 0 && ids.Contains(p.FarmerId))
            .GroupBy(p => p.FarmerId)
            .Select(g => new { FarmerId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.FarmerId, x => x.Count);

        // One row per (farmer, order, buyer) for orders that were really delivered.
        var delivered = (await _db.OrderItems
                .Where(i => i.Order!.Status == "Delivered" && ids.Contains(i.Product!.FarmerId))
                .Select(i => new { FarmerId = i.Product!.FarmerId, i.OrderId, BuyerId = i.Order!.BuyerId })
                .Distinct()
                .ToListAsync())
            .ToLookup(d => d.FarmerId);

        return farmers
            .Select(f =>
            {
                var rows = delivered[f.Id].ToList();
                var byBuyer = rows.GroupBy(d => d.BuyerId).ToList();

                return new FarmerSpotlight
                {
                    Id = f.Id,
                    Name = f.FullName,
                    PhotoUrl = f.ProfileImagePath,
                    City = f.City,
                    State = f.State,
                    Crops = (f.CropsGrown ?? "")
                        .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                        .Take(4).ToList(),
                    ExperienceYears = f.ExperienceYears,
                    JoinedAt = f.CreatedAt,
                    ProductCount = productCounts.TryGetValue(f.Id, out var c) ? c : 0,
                    DeliveredOrders = rows.Select(r => r.OrderId).Distinct().Count(),
                    Buyers = byBuyer.Count,
                    RepeatBuyers = byBuyer.Count(g => g.Select(x => x.OrderId).Distinct().Count() >= 2)
                };
            })
            .OrderByDescending(x => x.Points)
            .ThenByDescending(x => x.ProductCount)
            .ThenBy(x => x.JoinedAt)
            .Take(5)
            .ToList();
    }
}
