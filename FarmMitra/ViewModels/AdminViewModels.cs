using System.Globalization;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.ViewModels;

public interface IPaged
{
    int Page { get; }
    int TotalPages { get; }
    int TotalCount { get; }
    string? Q { get; }
    string? Status { get; }
}

public class PagedResult<T> : IPaged
{
    public List<T> Items { get; set; } = new();
    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
    public string? Q { get; set; }
    public string? Status { get; set; }

    // The query must already be ordered.
    public static async Task<PagedResult<T>> CreateAsync(IQueryable<T> query, int page, int pageSize, string? q, string? status)
    {
        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
        page = Math.Clamp(page, 1, totalPages);
        var items = await query.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        return new PagedResult<T> { Items = items, Page = page, TotalPages = totalPages, TotalCount = total, Q = q, Status = status };
    }
}

public class UserRow
{
    public string Id { get; set; } = "";
    public string FullName { get; set; } = "";
    public string? ProfileImagePath { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public string? FarmLocation { get; set; }
    public string? CropsGrown { get; set; }
    public decimal? FarmSizeAcres { get; set; }
    public int? ExperienceYears { get; set; }
    public DateTime CreatedAt { get; set; }
    public string ApprovalStatus { get; set; } = "";
    public bool IsApproved { get; set; }
    public int ProductCount { get; set; }
    public int OrderCount { get; set; }
}

public class OrderRow
{
    public int Id { get; set; }
    public string BuyerName { get; set; } = "";
    public string? BuyerEmail { get; set; }
    public int ItemCount { get; set; }
    public decimal Total { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "";
}

public class ComplaintRow
{
    public int Id { get; set; }
    public string UserName { get; set; } = "";
    public string Type { get; set; } = "";
    public string Subject { get; set; } = "";
    public string Description { get; set; } = "";
    public int? OrderId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string Status { get; set; } = "";
}

public class MonthBar
{
    public string Label { get; set; } = "";
    public decimal Value { get; set; }
}

public class BarChartModel
{
    public List<MonthBar> Bars { get; set; } = new();
    public string Color { get; set; } = "var(--primary-light)";
    public string Prefix { get; set; } = "";
}

public class CategorySlice
{
    public string Name { get; set; } = "";
    public int Count { get; set; }
    public double Percent { get; set; }
    public double Offset { get; set; }
    public string Dash => $"{Percent.ToString("0.##", CultureInfo.InvariantCulture)} {(100 - Percent).ToString("0.##", CultureInfo.InvariantCulture)}";
    public string DashOffset => (-Offset).ToString("0.##", CultureInfo.InvariantCulture);
}

public class AdminDashboardViewModel
{
    public int TotalFarmers { get; set; }
    public int TotalBuyers { get; set; }
    public int TotalProducts { get; set; }
    public int TotalOrders { get; set; }
    public int PendingApprovals { get; set; }
    public int OpenComplaints { get; set; }

    public List<MonthBar> MonthlyOrders { get; set; } = new();
    public List<MonthBar> MonthlySales { get; set; } = new();
    public List<MonthBar> FarmerRegistrations { get; set; } = new();
    public List<CategorySlice> Categories { get; set; } = new();

    public List<UserRow> PendingFarmers { get; set; } = new();
    public List<ComplaintRow> RecentComplaints { get; set; } = new();
}

public static class BadgeHelper
{
    public static string Css(string? status) => status switch
    {
        "Approved" or "Available" or "Delivered" or "Resolved" => "badge-success",
        "Pending" => "badge-warning",
        "Confirmed" or "Shipped" or "Under Review" => "badge-info",
        "Rejected" or "Cancelled" or "OutOfStock" => "badge-danger",
        _ => "badge-neutral"
    };

    public static string Label(string? status) => status == "OutOfStock" ? "Out of Stock" : status ?? "";
}
