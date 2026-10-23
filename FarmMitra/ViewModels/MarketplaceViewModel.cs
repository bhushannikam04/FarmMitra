using FarmMitra.Models;

namespace FarmMitra.ViewModels;

public class MarketplaceViewModel
{
    public List<Product> Products { get; set; } = new();
    public List<string> Categories { get; set; } = new();
    public List<string> Locations { get; set; } = new();

    public string? Q { get; set; }
    public string? Category { get; set; }
    public string? Location { get; set; }
    public decimal? MinPrice { get; set; }
    public decimal? MaxPrice { get; set; }
    public string? Sort { get; set; }

    public int Page { get; set; } = 1;
    public int TotalPages { get; set; } = 1;
    public int TotalCount { get; set; }
}

public class ProductDetailsViewModel
{
    public Product Product { get; set; } = null!;
    public List<Product> Related { get; set; } = new();
}
