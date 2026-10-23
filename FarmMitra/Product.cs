using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace FarmMitra.Models;

public class Product
{
    public int Id { get; set; }
    [Required, StringLength(100)] public string Name { get; set; } = "";
    [Required] public string Category { get; set; } = "";
    [Column(TypeName = "decimal(18,2)")] public decimal Price { get; set; }
    public int Quantity { get; set; }
    public string Unit { get; set; } = "Quintal";
    public string? Description { get; set; }
    public string? Location { get; set; }
    public string? ImagePath { get; set; }
    public string Status { get; set; } = "Available";   // Available / Pending / OutOfStock
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public string FarmerId { get; set; } = "";
    public ApplicationUser? Farmer { get; set; }
    [MaxLength(40)] public string? Grade { get; set; }
    [MaxLength(40)] public string? Size { get; set; }
    [MaxLength(40)] public string? Harvested { get; set; }
    [MaxLength(40)] public string? ShelfLife { get; set; }
    [MaxLength(40)] public string? MinOrder { get; set; }
    [MaxLength(60)] public string? Packing { get; set; }
}

public class CartItem
{
    public int Id { get; set; }
    public string BuyerId { get; set; } = "";
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; } = 1;
}

public class Order
{
    public string? DeliveryAddress { get; set; }
    public string PaymentMethod { get; set; } = "Cash on Delivery";
    [Column(TypeName = "decimal(18,2)")] public decimal DeliveryFee { get; set; }
    public int Id { get; set; }
    public string BuyerId { get; set; } = "";
    public ApplicationUser? Buyer { get; set; }
    public DateTime OrderDate { get; set; } = DateTime.UtcNow;
    public string Status { get; set; } = "Pending";   // Pending → Confirmed → Shipped → Delivered / Cancelled
    [Column(TypeName = "decimal(18,2)")] public decimal Total { get; set; }
    public List<OrderItem> Items { get; set; } = new();
}

public class OrderItem
{
    public int Id { get; set; }
    public int OrderId { get; set; }
    public Order? Order { get; set; }
    public int ProductId { get; set; }
    public Product? Product { get; set; }
    public int Quantity { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
}

public class Complaint
{
    public int Id { get; set; }
    public string UserId { get; set; } = "";
    public ApplicationUser? User { get; set; }
    public string Type { get; set; } = "";
    public string Subject { get; set; } = "";
    public int? OrderId { get; set; }
    public string Description { get; set; } = "";
    public string? AttachmentPath { get; set; }
    public string Status { get; set; } = "Pending";   // Pending / Under Review / Resolved / Rejected
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}