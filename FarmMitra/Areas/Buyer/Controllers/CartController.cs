using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.Services;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Areas.Buyer.Controllers;

public class CartController : BuyerBaseController
{
    private readonly SettingsService _settings;

    public CartController(ApplicationDbContext db, UserManager<ApplicationUser> users, SettingsService settings) : base(db, users)
        => _settings = settings;

    // ---------- CART ----------
    public async Task<IActionResult> Index() => View(await LoadCartAsync());

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Add(int productId, int quantity = 1, bool buyNow = false, string? returnUrl = null)
    {
        quantity = Math.Max(1, quantity);
        var uid = CurrentUser.Id;

        var product = await Db.Products.FirstOrDefaultAsync(p => p.Id == productId);
        if (product == null || product.Status != "Available" || product.Quantity <= 0)
        {
            TempData["Error"] = "Sorry, that product isn't available right now.";
            return Back(returnUrl, "Index", "Cart");
        }

        var line = await Db.CartItems.FirstOrDefaultAsync(c => c.BuyerId == uid && c.ProductId == productId);
        var wanted = (line?.Quantity ?? 0) + quantity;
        var capped = wanted > product.Quantity;
        if (capped) wanted = product.Quantity;

        if (line == null)
            Db.CartItems.Add(new CartItem { BuyerId = uid, ProductId = productId, Quantity = wanted });
        else
            line.Quantity = wanted;

        await Db.SaveChangesAsync();

        if (capped)
            TempData["Error"] = $"Only {product.Quantity} {product.Unit} of {product.Name} is available. Your cart has the maximum.";
        else
            TempData["Success"] = $"{product.Name} was added to your cart.";

        return buyNow ? RedirectToAction(nameof(Checkout)) : Back(returnUrl, "Index", "Cart");
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Update(int id, int quantity)
    {
        var uid = CurrentUser.Id;
        var line = await Db.CartItems.Include(c => c.Product).FirstOrDefaultAsync(c => c.Id == id && c.BuyerId == uid);
        if (line == null) return RedirectToAction(nameof(Index));

        if (quantity < 1)
            Db.CartItems.Remove(line);
        else
            line.Quantity = Math.Min(quantity, Math.Max(1, line.Product!.Quantity));

        await Db.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Remove(int id)
    {
        var uid = CurrentUser.Id;
        var line = await Db.CartItems.FirstOrDefaultAsync(c => c.Id == id && c.BuyerId == uid);
        if (line != null)
        {
            Db.CartItems.Remove(line);
            await Db.SaveChangesAsync();
            TempData["Success"] = "Item removed from your cart.";
        }
        return RedirectToAction(nameof(Index));
    }

    // ---------- CHECKOUT ----------
    [HttpGet]
    public async Task<IActionResult> Checkout()
    {
        var cart = await LoadCartAsync();
        var problem = CartProblem(cart);
        if (problem != null)
        {
            TempData["Error"] = problem;
            return RedirectToAction(nameof(Index));
        }

        return View(new CheckoutViewModel
        {
            Cart = cart,
            FullName = CurrentUser.FullName,
            Phone = CurrentUser.PhoneNumber ?? "",
            Address = CurrentUser.Address ?? "",
            City = CurrentUser.City ?? "",
            State = CurrentUser.State ?? ""
        });
    }

    [HttpPost, ValidateAntiForgeryToken]
    public async Task<IActionResult> Checkout(CheckoutViewModel model)
    {
        // Totals always come from the database, never from the posted form.
        var cart = await LoadCartAsync();
        var problem = CartProblem(cart);
        if (problem != null)
        {
            TempData["Error"] = problem;
            return RedirectToAction(nameof(Index));
        }

        if (!ModelState.IsValid)
        {
            model.Cart = cart;
            return View(model);
        }

        await using var tx = await Db.Database.BeginTransactionAsync();

        // Take the stock atomically: this only succeeds if enough is still on the shelf.
        foreach (var line in cart.Lines)
        {
            var productId = line.ProductId;
            var qty = line.Quantity;

            var updated = await Db.Products
                .Where(p => p.Id == productId && p.Status == "Available" && p.Quantity >= qty)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Quantity, p => p.Quantity - qty));

            if (updated == 0)
            {
                await tx.RollbackAsync();
                TempData["Error"] = $"Sorry, \"{line.Name}\" just sold out or doesn't have enough stock. Please review your cart.";
                return RedirectToAction(nameof(Index));
            }
        }

        var productIds = cart.Lines.Select(l => l.ProductId).ToList();
        await Db.Products
            .Where(p => productIds.Contains(p.Id) && p.Quantity == 0)
            .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "OutOfStock"));

        var order = new Order
        {
            BuyerId = CurrentUser.Id,
            Status = "Pending",
            OrderDate = DateTime.UtcNow,
            DeliveryFee = cart.DeliveryFee,
            Total = cart.Total,
            PaymentMethod = "Cash on Delivery",
            DeliveryAddress = $"{model.FullName.Trim()}, {model.Phone.Trim()}\n{model.Address.Trim()}, {model.City.Trim()}, {model.State}",
            Items = cart.Lines.Select(l => new OrderItem
            {
                ProductId = l.ProductId,
                Quantity = l.Quantity,
                UnitPrice = l.UnitPrice
            }).ToList()
        };
        Db.Orders.Add(order);

        var uid = CurrentUser.Id;
        Db.CartItems.RemoveRange(await Db.CartItems.Where(c => c.BuyerId == uid).ToListAsync());

        await Db.SaveChangesAsync();
        await tx.CommitAsync();

        TempData["Success"] = $"Order #FM-{order.Id} placed. The farmer will confirm it shortly.";
        return RedirectToAction("Details", "Orders", new { id = order.Id });
    }

    // ---------- HELPERS ----------
    private async Task<CartViewModel> LoadCartAsync()
    {
        var uid = CurrentUser.Id;
        var lines = await Db.CartItems
            .Where(c => c.BuyerId == uid)
            .OrderBy(c => c.Id)
            .Select(c => new CartLineRow
            {
                CartItemId = c.Id,
                ProductId = c.ProductId,
                Name = c.Product!.Name,
                ImagePath = c.Product.ImagePath,
                Unit = c.Product.Unit,
                FarmerName = c.Product.Farmer!.FullName,
                UnitPrice = c.Product.Price,
                Quantity = c.Quantity,
                Stock = c.Product.Quantity,
                Available = c.Product.Status == "Available" && c.Product.Quantity > 0
            })
            .ToListAsync();

        var site = await _settings.GetAsync();
        return new CartViewModel { Lines = lines, FlatFee = site.FlatDeliveryFee, FreeDeliveryAbove = site.FreeDeliveryAbove };
    }

    private static string? CartProblem(CartViewModel cart) =>
        !cart.Lines.Any() ? "Your cart is empty."
        : cart.HasIssues ? "Please fix the highlighted items in your cart before checking out."
        : null;
}
