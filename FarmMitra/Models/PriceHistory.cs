using System.ComponentModel.DataAnnotations.Schema;

namespace FarmMitra.Models;

// One row each time a product's price is set or changed. Powers the price trends.
public class PriceHistory
{
    public int Id { get; set; }

    public int ProductId { get; set; }
    public Product? Product { get; set; }

    [Column(TypeName = "decimal(18,2)")]
    public decimal Price { get; set; }

    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
