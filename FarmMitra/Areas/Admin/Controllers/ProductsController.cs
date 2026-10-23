using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Areas.Admin.Controllers;

public class ProductsController : AdminBaseController
{
    private const int PageSize = 10;
    private static readonly string[] ValidStatuses = { "Available", "Pending", "OutOfStock" };

    public ProductsController(ApplicationDbContext db) : base(db) { }

    public async Task<IActionResult> Index(string? q, string? status, int page = 1)
    {
        var query = Db.Products.Include(p => p.Farmer).AsQueryable();

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Name.Contains(q) || p.Category.Contains(q) || p.Farmer!.FullName.Contains(q));

        return View(await PagedResult<Product>.CreateAsync(query.OrderByDescending(p => p.CreatedAt), page, PageSize, q, status));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> SetStatus(int id, string status, string? returnUrl)
    {
        if (!ValidStatuses.Contains(status)) return BadRequest();

        var product = await Db.Products.FindAsync(id);
        if (product == null)
        {
            TempData["Error"] = "Product not found.";
            return Back(returnUrl, nameof(Index));
        }

        product.Status = status;
        await Db.SaveChangesAsync();
        TempData["Success"] = $"\"{product.Name}\" is now {BadgeHelper.Label(status).ToLower()}.";
        return Back(returnUrl, nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id, string? returnUrl)
    {
        var product = await Db.Products.FindAsync(id);
        if (product == null)
        {
            TempData["Error"] = "Product not found.";
            return Back(returnUrl, nameof(Index));
        }

        // Products that appear in past orders must be kept for order history.
        if (await Db.OrderItems.AnyAsync(i => i.ProductId == id))
        {
            TempData["Error"] = $"\"{product.Name}\" is part of existing orders and can't be deleted. Hide it instead.";
            return Back(returnUrl, nameof(Index));
        }

        var inCarts = await Db.CartItems.Where(c => c.ProductId == id).ToListAsync();
        Db.CartItems.RemoveRange(inCarts);
        Db.Products.Remove(product);
        await Db.SaveChangesAsync();

        TempData["Success"] = $"\"{product.Name}\" was deleted.";
        return Back(returnUrl, nameof(Index));
    }
}
