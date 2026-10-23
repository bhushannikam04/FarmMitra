using FarmMitra.Models;
using Microsoft.EntityFrameworkCore;

namespace FarmMitra.Data;

public static class OrderStock
{
    // Cancels an order and puts its quantities back on the shelf, all-or-nothing.
    public static async Task CancelAsync(ApplicationDbContext db, Order order)
    {
        await using var tx = await db.Database.BeginTransactionAsync();

        var lines = await db.OrderItems
            .Where(i => i.OrderId == order.Id)
            .Select(i => new { i.ProductId, i.Quantity })
            .ToListAsync();

        foreach (var line in lines)
        {
            var productId = line.ProductId;
            var qty = line.Quantity;

            await db.Products.Where(p => p.Id == productId)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Quantity, p => p.Quantity + qty));

            await db.Products.Where(p => p.Id == productId && p.Status == "OutOfStock" && p.Quantity > 0)
                .ExecuteUpdateAsync(s => s.SetProperty(p => p.Status, "Available"));
        }

        order.Status = "Cancelled";
        await db.SaveChangesAsync();
        await tx.CommitAsync();
    }
}
