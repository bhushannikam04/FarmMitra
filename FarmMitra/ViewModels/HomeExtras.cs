namespace FarmMitra.ViewModels;

/// <summary>One product in the live price strip and the savings calculator on the home page.</summary>
public class TickerItem
{
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public string Unit { get; set; } = "";
    public string? Location { get; set; }
}

/// <summary>Real platform numbers for the home page tiles (no made-up figures).</summary>
public class HomeStats
{
    public int Farmers { get; set; }
    public int Products { get; set; }
    public int DeliveredOrders { get; set; }
    public int Buyers { get; set; }
}
