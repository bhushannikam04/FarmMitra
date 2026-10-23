namespace FarmMitra.ViewModels;

public class InboxRow
{
    public int Id { get; set; }
    public string OtherName { get; set; } = "";
    public string? ProductName { get; set; }
    public string? LastMessage { get; set; }
    public DateTime LastMessageAt { get; set; }
    public int Unread { get; set; }
    public bool IsClosed { get; set; }
}

public class InboxViewModel
{
    public string Area { get; set; } = "";
    public List<InboxRow> Rows { get; set; } = new();
}

public class ChatMessageRow
{
    public int Id { get; set; }
    public string SenderId { get; set; } = "";
    public string SenderName { get; set; } = "";
    public string Body { get; set; } = "";
    public DateTime SentAt { get; set; }
    public bool IsHidden { get; set; }
}

public class ThreadViewModel
{
    public int Id { get; set; }
    public string Area { get; set; } = "";
    public string? ViewerId { get; set; }          // null when an admin is watching
    public bool IsAdmin { get; set; }

    public string BuyerId { get; set; } = "";
    public string BuyerName { get; set; } = "";
    public string FarmerId { get; set; } = "";
    public string FarmerName { get; set; } = "";

    public int? ProductId { get; set; }
    public string? ProductName { get; set; }

    public bool IsClosed { get; set; }
    public bool IsReported { get; set; }
    public string? ReportReason { get; set; }

    public List<ChatMessageRow> Messages { get; set; } = new();

    public string OtherName => ViewerId == BuyerId ? FarmerName : BuyerName;
}

public class AdminConversationRow
{
    public int Id { get; set; }
    public string BuyerName { get; set; } = "";
    public string FarmerName { get; set; } = "";
    public string? ProductName { get; set; }
    public int MessageCount { get; set; }
    public DateTime LastMessageAt { get; set; }
    public bool IsReported { get; set; }
    public string? ReportReason { get; set; }
    public bool IsClosed { get; set; }
}
