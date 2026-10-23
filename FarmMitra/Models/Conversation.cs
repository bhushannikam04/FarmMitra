using System.ComponentModel.DataAnnotations;

namespace FarmMitra.Models;

// One chat thread between a buyer and a farmer.
public class Conversation
{
    public int Id { get; set; }

    public string BuyerId { get; set; } = "";
    public ApplicationUser? Buyer { get; set; }

    public string FarmerId { get; set; } = "";
    public ApplicationUser? Farmer { get; set; }

    // The product the buyer was looking at when the chat started (optional).
    public int? ProductId { get; set; }
    public Product? Product { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime LastMessageAt { get; set; } = DateTime.UtcNow;

    // Moderation
    public bool IsReported { get; set; }
    [StringLength(300)] public string? ReportReason { get; set; }
    public bool IsClosed { get; set; }

    public List<Message> Messages { get; set; } = new();
}

public class Message
{
    public int Id { get; set; }

    public int ConversationId { get; set; }
    public Conversation? Conversation { get; set; }

    public string SenderId { get; set; } = "";
    public ApplicationUser? Sender { get; set; }

    [Required, StringLength(2000)]
    public string Body { get; set; } = "";

    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool IsRead { get; set; }

    // Set by an admin; participants then see "removed by an administrator".
    public bool IsHidden { get; set; }
}
