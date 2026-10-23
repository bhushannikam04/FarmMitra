using System.ComponentModel.DataAnnotations;
using FarmMitra.Models;
using Microsoft.AspNetCore.Http;

namespace FarmMitra.ViewModels;

public static class ProductOptions
{
    public static readonly string[] Categories = { "Vegetables", "Fruits", "Grains", "Pulses", "Spices", "Dairy", "Other" };
    public static readonly string[] Units = { "Quintal", "Kg", "Ton", "Dozen", "Piece" };
}

public static class OrderFlow
{
    // The next steps a farmer is allowed to move an order to.
    public static string[] Next(string status) => status switch
    {
        "Pending"   => new[] { "Confirmed", "Cancelled" },
        "Confirmed" => new[] { "Shipped", "Cancelled" },
        "Shipped"   => new[] { "Delivered" },
        _           => Array.Empty<string>()
    };
}

public class ProductFormViewModel
{
    public int Id { get; set; }

    [Required(ErrorMessage = "Enter the product name.")]
    [StringLength(100)]
    [Display(Name = "Product Name")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Choose a category.")]
    public string Category { get; set; } = "";

    [Required(ErrorMessage = "Enter the price.")]
    [Range(0.01, 10000000.0, ErrorMessage = "Enter a valid price.")]
    public decimal? Price { get; set; }

    [Required(ErrorMessage = "Enter the available quantity.")]
    [Range(0, 1000000, ErrorMessage = "Enter a valid quantity.")]
    public int? Quantity { get; set; }

    [Required(ErrorMessage = "Choose a unit.")]
    public string Unit { get; set; } = "Quintal";

    [StringLength(1000, ErrorMessage = "Keep the description under 1000 characters.")]
    public string? Description { get; set; }

    [StringLength(100)]
    public string? Location { get; set; }

    // ---- Specifications shown when a buyer hovers over the product image (all optional) ----
    [StringLength(40)] public string? Grade { get; set; }
    [StringLength(40)] public string? Size { get; set; }
    [StringLength(40)] public string? Harvested { get; set; }
    [StringLength(40), Display(Name = "Shelf life")] public string? ShelfLife { get; set; }
    [StringLength(40), Display(Name = "Minimum order")] public string? MinOrder { get; set; }
    [StringLength(60)] public string? Packing { get; set; }

    [Display(Name = "Product Image")]
    public IFormFile? Image { get; set; }

    public string? ExistingImagePath { get; set; }
}

public class FarmerOrderRow
{
    public int OrderId { get; set; }
    public string ProductName { get; set; } = "";
    public string? ProductImage { get; set; }
    public string BuyerName { get; set; } = "";
    public int Quantity { get; set; }
    public string Unit { get; set; } = "";
    public decimal Amount { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "";
    public bool OnlyMine { get; set; }
}

public class FarmerDashboardViewModel
{
    public string FirstName { get; set; } = "";
    public int TotalProducts { get; set; }
    public int PendingOrders { get; set; }
    public int CompletedOrders { get; set; }
    public decimal TotalSales { get; set; }
    public List<MonthBar> SalesChart { get; set; } = new();
    public List<Product> LowStock { get; set; } = new();
    public List<FarmerOrderRow> RecentOrders { get; set; } = new();
    public List<Product> RecentProducts { get; set; } = new();
}
