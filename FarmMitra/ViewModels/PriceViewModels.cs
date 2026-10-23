namespace FarmMitra.ViewModels;

public class Listing
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public string Unit { get; set; } = "";
    public decimal Price { get; set; }
    public string FarmerId { get; set; } = "";
    public string? ImagePath { get; set; }
}

public class PricePoint
{
    public string Key { get; set; } = "";
    public DateTime Date { get; set; }
    public decimal PerQuintal { get; set; }
}

public class PriceRow
{
    public int ProductId { get; set; }
    public string Name { get; set; } = "";
    public string? ImagePath { get; set; }
    public string Unit { get; set; } = "";
    public decimal MyPrice { get; set; }
    public decimal? MyPerQuintal { get; set; }
    public string Key { get; set; } = "";
    public string Commodity { get; set; } = "";

    public int OthersCount { get; set; }
    public decimal? MarketAvg { get; set; }
    public decimal? MarketMin { get; set; }
    public decimal? MarketMax { get; set; }
    public decimal? DiffPercent { get; set; }
    public string Verdict { get; set; } = "";
}

public class CommoditySummary
{
    public string Key { get; set; } = "";
    public string Commodity { get; set; } = "";
    public int Listings { get; set; }
    public int Sellers { get; set; }
    public decimal Avg { get; set; }
    public decimal Min { get; set; }
    public decimal Max { get; set; }
    public decimal? TrendPercent { get; set; }
}

public class PriceAnalysisViewModel
{
    public List<PriceRow> Rows { get; set; } = new();
    public List<CommoditySummary> Market { get; set; } = new();

    public string? SelectedKey { get; set; }
    public string? SelectedName { get; set; }
    public CommoditySummary? Selected { get; set; }
    public PriceRow? SelectedRow { get; set; }
    public decimal? SelectedTrend { get; set; }
    public List<MonthBar> Trend { get; set; } = new();
    public string? AdviceLevel { get; set; }
    public string? AdviceText { get; set; }
}
