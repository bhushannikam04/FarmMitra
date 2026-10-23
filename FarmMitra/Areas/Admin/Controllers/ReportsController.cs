using System.Text;
using FarmMitra.Data;
using FarmMitra.Services;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Areas.Admin.Controllers;

public class ReportsController : AdminBaseController
{
    public ReportsController(ApplicationDbContext db) : base(db) { }

    // ------------------------------------------------------------ report page
    public async Task<IActionResult> Index(string? range, DateTime? from, DateTime? to)
    {
        var (f, t) = ResolveRange(range, from, to);
        var end = t.AddDays(1);                                  // exclusive upper bound
        var days = (t - f).Days + 1;
        var prevTo = f.AddDays(-1);
        var prevFrom = f.AddDays(-days);

        var farmerIds = UserIdsInRole("Farmer");
        var buyerIds = UserIdsInRole("Buyer");

        // ---- orders
        var orders = await Db.Orders
            .Where(o => o.OrderDate >= f && o.OrderDate < end)
            .Select(o => new { o.OrderDate, o.Total, o.Status })
            .ToListAsync();

        var live = orders.Where(o => o.Status != "Cancelled").ToList();
        var orderValue = live.Sum(o => o.Total);

        var prevOrders = await Db.Orders
            .Where(o => o.OrderDate >= prevFrom && o.OrderDate < f)
            .Select(o => new { o.Total, o.Status })
            .ToListAsync();
        var prevLive = prevOrders.Where(o => o.Status != "Cancelled").ToList();

        // ---- order lines (cancelled orders excluded)
        var lines = await Db.OrderItems
            .Where(i => i.Order!.OrderDate >= f && i.Order.OrderDate < end && i.Order.Status != "Cancelled")
            .Select(i => new
            {
                i.OrderId,
                i.ProductId,
                ProductName = i.Product!.Name,
                i.Product.Unit,
                i.Product.Category,
                FarmerId = i.Product.FarmerId,
                FarmerName = i.Product.Farmer!.FullName,
                BuyerId = i.Order!.BuyerId,
                BuyerName = i.Order.Buyer!.FullName,
                i.Quantity,
                Amount = i.Quantity * i.UnitPrice
            })
            .ToListAsync();

        // ---- people, products, complaints
        var farmerDates = await Db.Users.Where(u => farmerIds.Contains(u.Id) && u.CreatedAt >= f && u.CreatedAt < end).Select(u => u.CreatedAt).ToListAsync();
        var buyerDates = await Db.Users.Where(u => buyerIds.Contains(u.Id) && u.CreatedAt >= f && u.CreatedAt < end).Select(u => u.CreatedAt).ToListAsync();
        var complaints = await Db.Complaints.Where(c => c.CreatedAt >= f && c.CreatedAt < end).Select(c => new { c.Status, c.Type }).ToListAsync();

        var buckets = Buckets(f, t);

        var vm = new ReportsViewModel
        {
            Range = range,
            From = f,
            To = t,
            Days = days,

            Orders = orders.Count,
            Delivered = orders.Count(o => o.Status == "Delivered"),
            Cancelled = orders.Count(o => o.Status == "Cancelled"),
            CancelRate = orders.Count == 0 ? 0 : Math.Round(orders.Count(o => o.Status == "Cancelled") * 100m / orders.Count, 1),
            OrderValue = orderValue,
            AvgOrderValue = live.Count == 0 ? 0 : Math.Round(orderValue / live.Count),
            OrdersChange = Change(orders.Count, prevOrders.Count),
            ValueChange = Change(orderValue, prevLive.Sum(o => o.Total)),

            NewFarmers = farmerDates.Count,
            NewBuyers = buyerDates.Count,
            NewProducts = await Db.Products.CountAsync(p => p.CreatedAt >= f && p.CreatedAt < end),
            Complaints = complaints.Count,

            OrdersChart = buckets.Select(b => new MonthBar { Label = b.Label, Value = orders.Count(o => o.OrderDate >= b.Start && o.OrderDate < b.End) }).ToList(),
            ValueChart = buckets.Select(b => new MonthBar { Label = b.Label, Value = Math.Round(live.Where(o => o.OrderDate >= b.Start && o.OrderDate < b.End).Sum(o => o.Total)) }).ToList(),
            FarmersChart = buckets.Select(b => new MonthBar { Label = b.Label, Value = farmerDates.Count(d => d >= b.Start && d < b.End) }).ToList(),
            BuyersChart = buckets.Select(b => new MonthBar { Label = b.Label, Value = buyerDates.Count(d => d >= b.Start && d < b.End) }).ToList(),

            StatusShare = Shares(orders.GroupBy(o => o.Status).Select(g => (g.Key, g.Count(), 0m))),
            CategoryShare = Shares(lines.GroupBy(l => l.Category).Select(g => (g.Key, g.Count(), g.Sum(x => x.Amount))), byAmount: true),
            ComplaintStatus = Shares(complaints.GroupBy(c => c.Status).Select(g => (g.Key, g.Count(), 0m))),
            ComplaintType = Shares(complaints.GroupBy(c => c.Type).Select(g => (g.Key, g.Count(), 0m))),

            TopProducts = lines.GroupBy(l => l.ProductId).Select(g => new RankRow
            {
                Name = g.First().ProductName,
                Sub = g.First().FarmerName,
                Orders = g.Select(x => x.OrderId).Distinct().Count(),
                Quantity = g.Sum(x => x.Quantity),
                Unit = g.First().Unit,
                Amount = g.Sum(x => x.Amount)
            }).OrderByDescending(r => r.Amount).Take(5).ToList(),

            TopFarmers = lines.GroupBy(l => l.FarmerId).Select(g => new RankRow
            {
                Name = g.First().FarmerName,
                Orders = g.Select(x => x.OrderId).Distinct().Count(),
                Amount = g.Sum(x => x.Amount)
            }).OrderByDescending(r => r.Amount).Take(5).ToList(),

            TopBuyers = lines.GroupBy(l => l.BuyerId).Select(g => new RankRow
            {
                Name = g.First().BuyerName,
                Orders = g.Select(x => x.OrderId).Distinct().Count(),
                Amount = g.Sum(x => x.Amount)
            }).OrderByDescending(r => r.Amount).Take(5).ToList(),

            TotalFarmers = await farmerIds.CountAsync(),
            TotalBuyers = await buyerIds.CountAsync(),
            TotalProducts = await Db.Products.CountAsync(),
            OutOfStock = await Db.Products.CountAsync(p => p.Status == "OutOfStock"),
            PendingApprovals = await Db.Users.CountAsync(u => u.ApprovalStatus == "Pending")
        };

        return View(vm);
    }

    // ------------------------------------------------------------ CSV downloads
    [HttpGet]
    public async Task<IActionResult> ExportOrders(string? range, DateTime? from, DateTime? to)
    {
        var (f, t) = ResolveRange(range, from, to);
        var end = t.AddDays(1);

        var rows = await Db.Orders
            .Where(o => o.OrderDate >= f && o.OrderDate < end)
            .OrderBy(o => o.OrderDate)
            .Select(o => new
            {
                o.Id, o.OrderDate, Buyer = o.Buyer!.FullName, Email = o.Buyer.Email,
                Items = o.Items.Count, o.DeliveryFee, o.Total, o.Status, o.PaymentMethod, o.DeliveryAddress
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine(Csv.Row("Order", "Date (UTC)", "Buyer", "Buyer email", "Items", "Delivery fee", "Total", "Status", "Payment", "Delivery address"));
        foreach (var r in rows)
            sb.AppendLine(Csv.Row("FM-" + r.Id, r.OrderDate, r.Buyer, r.Email, r.Items, r.DeliveryFee, r.Total, r.Status, r.PaymentMethod, r.DeliveryAddress));

        return CsvFile(sb, $"orders_{f:yyyyMMdd}-{t:yyyyMMdd}");
    }

    [HttpGet]
    public async Task<IActionResult> ExportComplaints(string? range, DateTime? from, DateTime? to)
    {
        var (f, t) = ResolveRange(range, from, to);
        var end = t.AddDays(1);

        var rows = await Db.Complaints
            .Where(c => c.CreatedAt >= f && c.CreatedAt < end)
            .OrderBy(c => c.CreatedAt)
            .Select(c => new { c.Id, c.CreatedAt, User = c.User!.FullName, c.Type, c.Subject, c.OrderId, c.Status, c.Description })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine(Csv.Row("Complaint", "Date (UTC)", "Raised by", "Type", "Subject", "Order", "Status", "Description"));
        foreach (var r in rows)
            sb.AppendLine(Csv.Row("CMP-" + r.Id, r.CreatedAt, r.User, r.Type, r.Subject, r.OrderId.HasValue ? "FM-" + r.OrderId : "", r.Status, r.Description));

        return CsvFile(sb, $"complaints_{f:yyyyMMdd}-{t:yyyyMMdd}");
    }

    [HttpGet]
    public async Task<IActionResult> ExportFarmers()
    {
        var farmerIds = UserIdsInRole("Farmer");

        var rows = await Db.Users
            .Where(u => farmerIds.Contains(u.Id))
            .OrderBy(u => u.FullName)
            .Select(u => new
            {
                u.FullName, u.Email, u.PhoneNumber, u.City, u.State, u.FarmLocation, u.FarmSizeAcres,
                u.CropsGrown, u.ExperienceYears, u.CreatedAt, u.ApprovalStatus,
                Products = Db.Products.Count(p => p.FarmerId == u.Id)
            })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine(Csv.Row("Name", "Email", "Mobile", "City", "State", "Farm location", "Farm size (acres)", "Crops", "Experience (years)", "Registered (UTC)", "Approval", "Products"));
        foreach (var r in rows)
            sb.AppendLine(Csv.Row(r.FullName, r.Email, r.PhoneNumber, r.City, r.State, r.FarmLocation, r.FarmSizeAcres, r.CropsGrown, r.ExperienceYears, r.CreatedAt, r.ApprovalStatus, r.Products));

        return CsvFile(sb, "farmers");
    }

    [HttpGet]
    public async Task<IActionResult> ExportProducts()
    {
        var rows = await Db.Products
            .OrderBy(p => p.Name)
            .Select(p => new { p.Name, p.Category, Farmer = p.Farmer!.FullName, p.Price, p.Unit, p.Quantity, p.Status, p.Location, p.CreatedAt })
            .ToListAsync();

        var sb = new StringBuilder();
        sb.AppendLine(Csv.Row("Product", "Category", "Farmer", "Price", "Unit", "Quantity", "Status", "Location", "Listed (UTC)"));
        foreach (var r in rows)
            sb.AppendLine(Csv.Row(r.Name, r.Category, r.Farmer, r.Price, r.Unit, r.Quantity, r.Status, r.Location, r.CreatedAt));

        return CsvFile(sb, "products");
    }

    // ------------------------------------------------------------ helpers
    // UTF-8 with a BOM so Excel shows rupee signs and local-language names correctly.
    private FileContentResult CsvFile(StringBuilder csv, string name)
    {
        var bytes = Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(csv.ToString())).ToArray();
        return File(bytes, "text/csv; charset=utf-8", $"farmmitra-{name}.csv");
    }

    private static (DateTime From, DateTime To) ResolveRange(string? range, DateTime? from, DateTime? to)
    {
        var today = DateTime.UtcNow.Date;
        DateTime f, t;

        switch (range)
        {
            case "7":   f = today.AddDays(-6);  t = today; break;
            case "90":  f = today.AddDays(-89); t = today; break;
            case "ytd": f = new DateTime(today.Year, 1, 1); t = today; break;
            case "30":  f = today.AddDays(-29); t = today; break;
            default:
                t = (to ?? today).Date;
                f = (from ?? t.AddDays(-29)).Date;
                break;
        }

        if (t > today) t = today;
        if (f > t) (f, t) = (t, f);
        if ((t - f).TotalDays > 730) f = t.AddDays(-730);
        return (f, t);
    }

    // Short ranges: one bar per day. Up to ~4 months: per week. Longer: per month.
    private static List<(DateTime Start, DateTime End, string Label)> Buckets(DateTime from, DateTime to)
    {
        var list = new List<(DateTime, DateTime, string)>();
        var days = (to - from).Days + 1;

        if (days <= 14)
        {
            for (var d = from; d <= to; d = d.AddDays(1))
                list.Add((d, d.AddDays(1), d.ToString("dd MMM")));
        }
        else if (days <= 120)
        {
            for (var s = from; s <= to; s = s.AddDays(7))
                list.Add((s, s.AddDays(7), s.ToString("dd MMM")));
        }
        else
        {
            for (var s = new DateTime(from.Year, from.Month, 1); s <= to; s = s.AddMonths(1))
                list.Add((s, s.AddMonths(1), s.ToString("MMM yy")));
        }

        return list;
    }

    private static decimal? Change(decimal now, decimal before) =>
        before == 0 ? null : Math.Round((now - before) / before * 100, 1);

    private static List<ShareRow> Shares(IEnumerable<(string Label, int Count, decimal Amount)> items, bool byAmount = false)
    {
        var list = items.ToList();
        var total = byAmount ? list.Sum(i => i.Amount) : list.Sum(i => (decimal)i.Count);

        return list
            .Select(i => new ShareRow
            {
                Label = i.Label,
                Count = i.Count,
                Amount = i.Amount,
                Percent = total == 0 ? 0 : (int)Math.Round((byAmount ? i.Amount : i.Count) * 100m / total)
            })
            .OrderByDescending(s => byAmount ? s.Amount : s.Count)
            .ToList();
    }
}
