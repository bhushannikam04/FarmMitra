using System.ComponentModel.DataAnnotations;
using FarmMitra.Models;
using Microsoft.AspNetCore.Http;

namespace FarmMitra.ViewModels;

public static class Pricing
{
    public const decimal FreeDeliveryAbove = 10000m;
    public const decimal DeliveryFlat = 200m;

    public static decimal Delivery(decimal subtotal) => Delivery(subtotal, DeliveryFlat, FreeDeliveryAbove);

    public static decimal Delivery(decimal subtotal, decimal flatFee, decimal freeAbove) =>
        subtotal <= 0 || subtotal >= freeAbove ? 0 : flatFee;
}

public static class ComplaintTypes
{
    public static readonly string[] All =
    {
        "Damaged product", "Wrong item received", "Quantity mismatch",
        "Late delivery", "Poor quality", "Other"
    };
}

public class CartLineRow
{
    public int CartItemId { get; set; }
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public string? ImagePath { get; set; }
    public string Unit { get; set; } = "";
    public string FarmerName { get; set; } = "";
    public decimal UnitPrice { get; set; }
    public int Quantity { get; set; }
    public int Stock { get; set; }
    public bool Available { get; set; }

    public decimal LineTotal => UnitPrice * Quantity;
    public string? Issue =>
        !Available ? "No longer available"
        : Quantity > Stock ? $"Only {Stock} {Unit} left"
        : null;
}

public class CartViewModel
{
    public List<CartLineRow> Lines { get; set; } = new();
    public decimal Subtotal => Lines.Sum(l => l.LineTotal);
    public decimal FlatFee { get; set; } = Pricing.DeliveryFlat;
    public decimal FreeDeliveryAbove { get; set; } = Pricing.FreeDeliveryAbove;
    public decimal DeliveryFee => Pricing.Delivery(Subtotal, FlatFee, FreeDeliveryAbove);
    public decimal Total => Subtotal + DeliveryFee;
    public bool HasIssues => Lines.Any(l => l.Issue != null);
}

public class CheckoutViewModel
{
    [Required(ErrorMessage = "Enter the name for delivery.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Enter a mobile number.")]
    [Phone(ErrorMessage = "Enter a valid mobile number.")]
    [Display(Name = "Mobile Number")]
    public string Phone { get; set; } = "";

    [Required(ErrorMessage = "Enter the delivery address.")]
    public string Address { get; set; } = "";

    [Required(ErrorMessage = "Enter the city.")]
    public string City { get; set; } = "";

    [Required(ErrorMessage = "Choose the state.")]
    public string State { get; set; } = "";

    public CartViewModel? Cart { get; set; }
}

public class OrderListRow
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "";
    public decimal Total { get; set; }
    public int ItemCount { get; set; }
    public string? FirstProduct { get; set; }
}

public class OrderLineRow
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public string? ImagePath { get; set; }
    public string Unit { get; set; } = "";
    public string FarmerName { get; set; } = "";
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal LineTotal => UnitPrice * Quantity;
}

public class OrderDetailsViewModel
{
    public int Id { get; set; }
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = "";
    public decimal Total { get; set; }
    public decimal DeliveryFee { get; set; }
    public string? DeliveryAddress { get; set; }
    public string PaymentMethod { get; set; } = "";
    public List<OrderLineRow> Lines { get; set; } = new();
    public decimal Subtotal => Lines.Sum(l => l.LineTotal);
}

public class BuyerDashboardViewModel
{
    public string FirstName { get; set; } = "";
    public int CartItems { get; set; }
    public int ActiveOrders { get; set; }
    public int DeliveredOrders { get; set; }
    public decimal TotalPurchases { get; set; }
    public List<OrderListRow> ActiveOrderRows { get; set; } = new();
    public List<Product> Recommended { get; set; } = new();
}

public class ComplaintFormViewModel
{
    [Required(ErrorMessage = "Choose a complaint type.")]
    public string Type { get; set; } = "";

    [Display(Name = "Order")]
    public int? OrderId { get; set; }

    [Required(ErrorMessage = "Enter a short subject.")]
    [StringLength(150, ErrorMessage = "Keep the subject under 150 characters.")]
    public string Subject { get; set; } = "";

    [Required(ErrorMessage = "Describe what went wrong.")]
    [StringLength(2000, ErrorMessage = "Keep the description under 2000 characters.")]
    public string Description { get; set; } = "";

    public IFormFile? Attachment { get; set; }
}

public class OrderOption
{
    public int Id { get; set; }
    public string Label { get; set; } = "";
}

public class ComplaintsPageViewModel
{
    public ComplaintFormViewModel Form { get; set; } = new();
    public List<OrderOption> Orders { get; set; } = new();
    public PagedResult<ComplaintRow> List { get; set; } = new();
}
