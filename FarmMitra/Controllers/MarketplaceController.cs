using FarmMitra.Data;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Controllers;

public class MarketplaceController : Controller
{
    private const int PageSize = 9;
    private readonly ApplicationDbContext _db;

    public MarketplaceController(ApplicationDbContext db) => _db = db;

    public async Task<IActionResult> Index(string? q, string? category, string? location,
        decimal? minPrice, decimal? maxPrice, string? sort, int page = 1)
    {
        var query = _db.Products
            .Include(p => p.Farmer)
            .Where(p => p.Status != "Pending");

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Name.Contains(q) || p.Farmer!.FullName.Contains(q));

        if (!string.IsNullOrWhiteSpace(category))
            query = query.Where(p => p.Category == category);

        if (!string.IsNullOrWhiteSpace(location))
            query = query.Where(p => p.Location == location);

        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

        query = sort switch
        {
            "price_asc"  => query.OrderBy(p => p.Price),
            "price_desc" => query.OrderByDescending(p => p.Price),
            "name"       => query.OrderBy(p => p.Name),
            _            => query.OrderByDescending(p => p.CreatedAt).ThenBy(p => p.Name)
        };

        var total = await query.CountAsync();
        var totalPages = Math.Max(1, (int)Math.Ceiling(total / (double)PageSize));
        page = Math.Clamp(page, 1, totalPages);

        var vm = new MarketplaceViewModel
        {
            Products = await query.Skip((page - 1) * PageSize).Take(PageSize).ToListAsync(),
            Categories = await _db.Products.Select(p => p.Category).Distinct().OrderBy(c => c).ToListAsync(),
            Locations = await _db.Products.Where(p => p.Location != null)
                                .Select(p => p.Location!).Distinct().OrderBy(l => l).ToListAsync(),
            Q = q, Category = category, Location = location,
            MinPrice = minPrice, MaxPrice = maxPrice, Sort = sort,
            Page = page, TotalPages = totalPages, TotalCount = total
        };
        return View(vm);
    }

    public async Task<IActionResult> Details(int id)
    {
        var product = await _db.Products.Include(p => p.Farmer).FirstOrDefaultAsync(p => p.Id == id);
        if (product == null) return NotFound();

        var related = await _db.Products.Include(p => p.Farmer)
            .Where(p => p.Category == product.Category && p.Id != id && p.Status != "Pending")
            .Take(3).ToListAsync();

        return View(new ProductDetailsViewModel { Product = product, Related = related });
    }
}
