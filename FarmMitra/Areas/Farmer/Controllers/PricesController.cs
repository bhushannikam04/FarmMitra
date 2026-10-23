using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.Services;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Areas.Farmer.Controllers;

public class PricesController : FarmerBaseController
{
    public PricesController(ApplicationDbContext db, UserManager<ApplicationUser> users) : base(db, users) { }

    public async Task<IActionResult> Index(string? key)
    {
        var uid = CurrentUser.Id;
        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1).AddMonths(-5);

        var market = await Db.Products
            .Where(p => p.Status == "Available" && p.Quantity > 0)
            .Select(p => new Listing { Id = p.Id, Name = p.Name, Unit = p.Unit, Price = p.Price, FarmerId = p.FarmerId, ImagePath = p.ImagePath })
            .ToListAsync();

        var mine = await Db.Products
            .Where(p => p.FarmerId == uid)
            .OrderBy(p => p.Name)
            .Select(p => new Listing { Id = p.Id, Name = p.Name, Unit = p.Unit, Price = p.Price, FarmerId = p.FarmerId, ImagePath = p.ImagePath })
            .ToListAsync();

        // Price points: every price change plus the prices actually paid in orders.
        var changes = await Db.PriceHistories
            .Where(h => h.ChangedAt >= start)
            .Select(h => new { h.Product!.Name, h.Product.Unit, h.Price, Date = h.ChangedAt })
            .ToListAsync();

        var sales = await Db.OrderItems
            .Where(i => i.Order!.OrderDate >= start && i.Order.Status != "Cancelled")
            .Select(i => new { i.Product!.Name, i.Product.Unit, Price = i.UnitPrice, Date = i.Order!.OrderDate })
            .ToListAsync();

        var points = changes.Select(c => (c.Name, c.Unit, c.Price, c.Date))
            .Concat(sales.Select(s => (s.Name, s.Unit, s.Price, s.Date)))
            .Select(x => new { Key = PriceAnalyzer.CommodityKey(x.Name), Q = PriceAnalyzer.ToPerQuintal(x.Price, x.Unit), x.Date })
            .Where(x => x.Key != "" && x.Q.HasValue)
            .Select(x => new PricePoint { Key = x.Key, Date = x.Date, PerQuintal = x.Q!.Value })
            .ToList();

        var vm = new PriceAnalysisViewModel
        {
            Rows = PriceAnalyzer.BuildRows(mine, market, uid),
            Market = PriceAnalyzer.BuildMarket(market, points, now)
        };

        // Which product to show in detail: the one asked for, else the farmer's first, else the busiest in the market.
        var knownKeys = vm.Market.Select(m => m.Key).Concat(vm.Rows.Select(r => r.Key)).ToHashSet();
        vm.SelectedKey = !string.IsNullOrEmpty(key) && knownKeys.Contains(key)
            ? key
            : vm.Rows.FirstOrDefault()?.Key ?? vm.Market.FirstOrDefault()?.Key;

        if (vm.SelectedKey != null)
        {
            vm.Selected = vm.Market.FirstOrDefault(m => m.Key == vm.SelectedKey);
            vm.SelectedRow = vm.Rows.FirstOrDefault(r => r.Key == vm.SelectedKey);
            vm.SelectedName = vm.Selected?.Commodity ?? vm.SelectedRow?.Commodity;
            vm.SelectedTrend = PriceAnalyzer.TrendPercent(points, vm.SelectedKey, now);
            vm.Trend = PriceAnalyzer.Trend(points, vm.SelectedKey, now);

            if (vm.SelectedRow != null)
            {
                var (level, text) = PriceAnalyzer.Advice(vm.SelectedRow);
                vm.AdviceLevel = level;
                vm.AdviceText = text;
            }
        }

        return View(vm);
    }
}
