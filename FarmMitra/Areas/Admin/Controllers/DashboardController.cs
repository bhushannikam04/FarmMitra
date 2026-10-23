using FarmMitra.Data;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Areas.Admin.Controllers;

public class DashboardController : AdminBaseController
{
    public DashboardController(ApplicationDbContext db) : base(db) { }

    public async Task<IActionResult> Index()
    {
        var farmerIds = UserIdsInRole("Farmer");
        var buyerIds = UserIdsInRole("Buyer");

        var now = DateTime.UtcNow;
        var start = new DateTime(now.Year, now.Month, 1).AddMonths(-5);
        var months = Enumerable.Range(0, 6).Select(i => start.AddMonths(i)).ToList();

        var recentOrders = await Db.Orders
            .Where(o => o.OrderDate >= start)
            .Select(o => new { o.OrderDate, o.Total, o.Status })
            .ToListAsync();

        var farmerDates = await Db.Users
            .Where(u => farmerIds.Contains(u.Id) && u.CreatedAt >= start)
            .Select(u => u.CreatedAt)
            .ToListAsync();

        var groups = await Db.Products
            .GroupBy(p => p.Category)
            .Select(g => new { Name = g.Key, Count = g.Count() })
            .OrderByDescending(g => g.Count)
            .ToListAsync();

        var totalProducts = groups.Sum(g => g.Count);
        var slices = new List<CategorySlice>();
        double offset = 0;
        foreach (var g in groups)
        {
            var pct = totalProducts == 0 ? 0 : g.Count * 100.0 / totalProducts;
            slices.Add(new CategorySlice { Name = g.Name, Count = g.Count, Percent = pct, Offset = offset });
            offset += pct;
        }

        var vm = new AdminDashboardViewModel
        {
            TotalFarmers = await farmerIds.CountAsync(),
            TotalBuyers = await buyerIds.CountAsync(),
            TotalProducts = totalProducts,
            TotalOrders = await Db.Orders.CountAsync(),
            PendingApprovals = await Db.Users.CountAsync(u => u.ApprovalStatus == "Pending"),
            OpenComplaints = await Db.Complaints.CountAsync(c => c.Status == "Pending" || c.Status == "Under Review"),

            MonthlyOrders = months.Select(m => new MonthBar
            {
                Label = m.ToString("MMM"),
                Value = recentOrders.Count(o => o.OrderDate.Year == m.Year && o.OrderDate.Month == m.Month)
            }).ToList(),

            MonthlySales = months.Select(m => new MonthBar
            {
                Label = m.ToString("MMM"),
                Value = recentOrders.Where(o => o.Status != "Cancelled" && o.OrderDate.Year == m.Year && o.OrderDate.Month == m.Month).Sum(o => o.Total)
            }).ToList(),

            FarmerRegistrations = months.Select(m => new MonthBar
            {
                Label = m.ToString("MMM"),
                Value = farmerDates.Count(d => d.Year == m.Year && d.Month == m.Month)
            }).ToList(),

            Categories = slices,

            PendingFarmers = await ToRows(Db.Users
                    .Where(u => farmerIds.Contains(u.Id) && u.ApprovalStatus == "Pending")
                    .OrderByDescending(u => u.CreatedAt)
                    .Take(5))
                .ToListAsync(),

            RecentComplaints = await Db.Complaints
                .OrderByDescending(c => c.CreatedAt)
                .Take(5)
                .Select(c => new ComplaintRow
                {
                    Id = c.Id,
                    UserName = c.User!.FullName,
                    Type = c.Type,
                    Subject = c.Subject,
                    CreatedAt = c.CreatedAt,
                    Status = c.Status
                })
                .ToListAsync()
        };

        return View(vm);
    }
}
