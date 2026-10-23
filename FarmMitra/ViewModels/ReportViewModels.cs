namespace FarmMitra.ViewModels;

public class RankRow
{
    public string Name { get; set; } = "";
    public string? Sub { get; set; }
    public int Orders { get; set; }
    public decimal Quantity { get; set; }
    public string? Unit { get; set; }
    public decimal Amount { get; set; }
}

public class ShareRow
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
    public decimal Amount { get; set; }
    public int Percent { get; set; }
}

public class ReportsViewModel
{
    public string? Range { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int Days { get; set; }

    public int Orders { get; set; }
    public int Delivered { get; set; }
    public int Cancelled { get; set; }
    public decimal CancelRate { get; set; }
    public decimal OrderValue { get; set; }
    public decimal AvgOrderValue { get; set; }
    public decimal? OrdersChange { get; set; }
    public decimal? ValueChange { get; set; }

    public int NewFarmers { get; set; }
    public int NewBuyers { get; set; }
    public int NewProducts { get; set; }
    public int Complaints { get; set; }

    public List<MonthBar> OrdersChart { get; set; } = new();
    public List<MonthBar> ValueChart { get; set; } = new();
    public List<MonthBar> FarmersChart { get; set; } = new();
    public List<MonthBar> BuyersChart { get; set; } = new();

    public List<ShareRow> StatusShare { get; set; } = new();
    public List<ShareRow> CategoryShare { get; set; } = new();
    public List<ShareRow> ComplaintStatus { get; set; } = new();
    public List<ShareRow> ComplaintType { get; set; } = new();

    public List<RankRow> TopProducts { get; set; } = new();
    public List<RankRow> TopFarmers { get; set; } = new();
    public List<RankRow> TopBuyers { get; set; } = new();

    public int TotalFarmers { get; set; }
    public int TotalBuyers { get; set; }
    public int TotalProducts { get; set; }
    public int OutOfStock { get; set; }
    public int PendingApprovals { get; set; }
}
