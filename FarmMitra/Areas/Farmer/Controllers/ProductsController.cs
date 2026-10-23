using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Areas.Farmer.Controllers;

public class ProductsController : FarmerBaseController
{
    private const int PageSize = 10;
    private const long MaxImageBytes = 3 * 1024 * 1024;
    private const string UploadUrlPrefix = "/uploads/products/";
    private static readonly string[] AllowedExtensions = { ".jpg", ".jpeg", ".png", ".webp" };

    private readonly IWebHostEnvironment _env;

    public ProductsController(ApplicationDbContext db, UserManager<ApplicationUser> users, IWebHostEnvironment env)
        : base(db, users) => _env = env;

    // ---------- LIST ----------
    public async Task<IActionResult> Index(string? q, string? status, int page = 1)
    {
        var uid = CurrentUser.Id;
        var query = Db.Products.Where(p => p.FarmerId == uid);

        if (!string.IsNullOrWhiteSpace(status))
            query = query.Where(p => p.Status == status);

        if (!string.IsNullOrWhiteSpace(q))
            query = query.Where(p => p.Name.Contains(q) || p.Category.Contains(q));

        return View(await PagedResult<Product>.CreateAsync(query.OrderByDescending(p => p.CreatedAt), page, PageSize, q, status));
    }

    // ---------- CREATE ----------
    [HttpGet]
    public IActionResult Create() =>
        View("Form", new ProductFormViewModel { Location = DefaultLocation() });

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ProductFormViewModel model)
    {
        ValidateImage(model.Image);
        if (!ModelState.IsValid) return View("Form", model);

        var product = new Product
        {
            Name = model.Name.Trim(),
            Category = model.Category,
            Price = model.Price!.Value,
            Quantity = model.Quantity!.Value,
            Unit = model.Unit,
            Description = model.Description?.Trim(),
            Location = string.IsNullOrWhiteSpace(model.Location) ? DefaultLocation() : model.Location.Trim(),
            Grade = Clean(model.Grade), Size = Clean(model.Size), Harvested = Clean(model.Harvested),
            ShelfLife = Clean(model.ShelfLife), MinOrder = Clean(model.MinOrder), Packing = Clean(model.Packing),
            FarmerId = CurrentUser.Id,
            Status = model.Quantity!.Value > 0 ? "Available" : "OutOfStock"
        };

        if (model.Image is { Length: > 0 })
            product.ImagePath = await SaveImageAsync(model.Image);

        Db.Products.Add(product);
        Db.PriceHistories.Add(new PriceHistory { Product = product, Price = product.Price });
        await Db.SaveChangesAsync();

        TempData["Success"] = $"\"{product.Name}\" is now listed in the marketplace.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- EDIT ----------
    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        var product = await FindMineAsync(id);
        if (product == null) return NotFound();

        return View("Form", new ProductFormViewModel
        {
            Id = product.Id,
            Name = product.Name,
            Category = product.Category,
            Price = product.Price,
            Quantity = product.Quantity,
            Unit = product.Unit,
            Description = product.Description,
            Location = product.Location,
            Grade = product.Grade, Size = product.Size, Harvested = product.Harvested,
            ShelfLife = product.ShelfLife, MinOrder = product.MinOrder, Packing = product.Packing,
            ExistingImagePath = product.ImagePath
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ProductFormViewModel model)
    {
        var product = await FindMineAsync(id);
        if (product == null) return NotFound();

        model.Id = id;
        model.ExistingImagePath = product.ImagePath;

        ValidateImage(model.Image);
        if (!ModelState.IsValid) return View("Form", model);

        product.Name = model.Name.Trim();
        product.Category = model.Category;
        if (product.Price != model.Price!.Value)
            Db.PriceHistories.Add(new PriceHistory { ProductId = product.Id, Price = model.Price!.Value });
        product.Price = model.Price!.Value;
        product.Quantity = model.Quantity!.Value;
        product.Unit = model.Unit;
        product.Description = model.Description?.Trim();
        product.Location = string.IsNullOrWhiteSpace(model.Location) ? DefaultLocation() : model.Location.Trim();
        product.Grade = Clean(model.Grade); product.Size = Clean(model.Size); product.Harvested = Clean(model.Harvested);
        product.ShelfLife = Clean(model.ShelfLife); product.MinOrder = Clean(model.MinOrder); product.Packing = Clean(model.Packing);

        // Stock drives availability. A product hidden by an admin ("Pending") stays hidden.
        if (product.Quantity <= 0) product.Status = "OutOfStock";
        else if (product.Status == "OutOfStock") product.Status = "Available";

        if (model.Image is { Length: > 0 })
        {
            var oldPath = product.ImagePath;
            product.ImagePath = await SaveImageAsync(model.Image);
            DeleteImageFile(oldPath);
        }

        await Db.SaveChangesAsync();
        TempData["Success"] = $"\"{product.Name}\" was updated.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- DELETE ----------
    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var product = await FindMineAsync(id);
        if (product == null) return NotFound();

        if (await Db.OrderItems.AnyAsync(i => i.ProductId == id))
        {
            TempData["Error"] = $"\"{product.Name}\" is part of existing orders and can't be deleted. Set its quantity to 0 to mark it out of stock instead.";
            return RedirectToAction(nameof(Index));
        }

        Db.CartItems.RemoveRange(await Db.CartItems.Where(c => c.ProductId == id).ToListAsync());
        Db.Products.Remove(product);
        await Db.SaveChangesAsync();
        DeleteImageFile(product.ImagePath);

        TempData["Success"] = $"\"{product.Name}\" was deleted.";
        return RedirectToAction(nameof(Index));
    }

    // ---------- HELPERS ----------
    private Task<Product?> FindMineAsync(int id)
    {
        var uid = CurrentUser.Id;
        return Db.Products.FirstOrDefaultAsync(p => p.Id == id && p.FarmerId == uid);
    }

    private static string? Clean(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

    private string? DefaultLocation()
    {
        var parts = new[] { CurrentUser.City, CurrentUser.State }.Where(s => !string.IsNullOrWhiteSpace(s));
        var text = string.Join(", ", parts);
        return text.Length > 0 ? text : CurrentUser.FarmLocation;
    }

    private void ValidateImage(IFormFile? file)
    {
        if (file == null || file.Length == 0) return;
        var key = nameof(ProductFormViewModel.Image);

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!AllowedExtensions.Contains(ext))
        {
            ModelState.AddModelError(key, "Please upload a JPG, PNG or WebP image.");
            return;
        }

        if (file.Length > MaxImageBytes)
        {
            ModelState.AddModelError(key, "The image must be 3 MB or smaller.");
            return;
        }

        // Check the real file signature, not just the name.
        var header = new byte[12];
        using (var stream = file.OpenReadStream())
            stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);

        var isJpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        var isWebp = header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                  && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

        if (!isJpeg && !isPng && !isWebp)
            ModelState.AddModelError(key, "That file doesn't look like a valid image.");
    }

    private async Task<string> SaveImageAsync(IFormFile file)
    {
        var folder = Path.Combine(_env.WebRootPath, "uploads", "products");
        Directory.CreateDirectory(folder);

        var fileName = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = new FileStream(Path.Combine(folder, fileName), FileMode.Create);
        await file.CopyToAsync(stream);

        return UploadUrlPrefix + fileName;
    }

    // Only ever deletes files this app uploaded (never external image URLs).
    private void DeleteImageFile(string? imagePath)
    {
        if (string.IsNullOrEmpty(imagePath) || !imagePath.StartsWith(UploadUrlPrefix)) return;

        var folder = Path.GetFullPath(Path.Combine(_env.WebRootPath, "uploads", "products"));
        var full = Path.GetFullPath(Path.Combine(_env.WebRootPath, imagePath.TrimStart('/')));
        if (full.StartsWith(folder) && System.IO.File.Exists(full))
            System.IO.File.Delete(full);
    }
}
