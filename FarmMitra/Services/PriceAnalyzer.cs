using System.Text.RegularExpressions;
using FarmMitra.ViewModels;

namespace FarmMitra.Services;

// Price comparisons built only from Farm Mitra's own data: current listings, price changes and orders.
public static class PriceAnalyzer
{
    private const decimal Tolerance = 15m;   // within +/-15% of the market average counts as "in line"

    // "Fresh Nashik Onions" -> "onion", "Nashik Green Grapes" -> "grape": the last word names the product.
    public static string CommodityKey(string name)
    {
        var words = Regex.Split(name.ToLowerInvariant(), "[^a-z]+").Where(w => w.Length > 0).ToList();
        if (words.Count == 0) return "";

        var w = words[^1];
        if (w.EndsWith("ies") && w.Length > 4) w = w[..^2];                       // chillies -> chilli
        else if (w.EndsWith("oes") && w.Length > 4) w = w[..^2];                  // tomatoes -> tomato
        else if (w.EndsWith("s") && !w.EndsWith("ss") && w.Length > 3) w = w[..^1];
        return Regex.Replace(w, "y$", "i");                                       // cherry / cherries -> cherri
    }

    public static string DisplayName(string name)
    {
        var words = Regex.Split(name, "[^A-Za-z]+").Where(w => w.Length > 0).ToList();
        if (words.Count == 0) return name;
        var w = words[^1];
        return char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant();
    }

    // Everything is compared as rupees per quintal. Dozen / piece prices can't be converted.
    public static decimal? ToPerQuintal(decimal price, string unit) => unit switch
    {
        "Quintal" => price,
        "Kg" => price * 100,
        "Ton" => price / 10,
        _ => null
    };

    public static List<PriceRow> BuildRows(List<Listing> mine, List<Listing> market, string farmerId)
    {
        var rows = new List<PriceRow>();

        foreach (var p in mine)
        {
            var key = CommodityKey(p.Name);
            var mineQ = ToPerQuintal(p.Price, p.Unit);

            var others = market
                .Where(m => m.FarmerId != farmerId && CommodityKey(m.Name) == key)
                .Select(m => ToPerQuintal(m.Price, m.Unit))
                .Where(v => v.HasValue)
                .Select(v => v!.Value)
                .ToList();

            var row = new PriceRow
            {
                ProductId = p.Id, Name = p.Name, ImagePath = p.ImagePath, Unit = p.Unit,
                MyPrice = p.Price, MyPerQuintal = mineQ, Key = key, Commodity = DisplayName(p.Name)
            };

            if (mineQ == null)
            {
                row.Verdict = "Not comparable";
            }
            else if (others.Count == 0)
            {
                row.Verdict = "No other sellers";
            }
            else
            {
                var avg = others.Average();
                row.OthersCount = others.Count;
                row.MarketAvg = Math.Round(avg);
                row.MarketMin = Math.Round(others.Min());
                row.MarketMax = Math.Round(others.Max());
                row.DiffPercent = Math.Round((mineQ.Value - avg) / avg * 100, 1);
                row.Verdict = row.DiffPercent > Tolerance ? "Above market"
                            : row.DiffPercent < -Tolerance ? "Below market"
                            : "In line";
            }

            rows.Add(row);
        }

        return rows;
    }

    public static List<CommoditySummary> BuildMarket(List<Listing> market, List<PricePoint> points, DateTime now)
    {
        return market
            .Select(m => new { m.FarmerId, m.Name, Q = ToPerQuintal(m.Price, m.Unit), Key = CommodityKey(m.Name) })
            .Where(x => x.Q.HasValue && x.Key != "")
            .GroupBy(x => x.Key)
            .Select(g => new CommoditySummary
            {
                Key = g.Key,
                Commodity = DisplayName(g.First().Name),
                Listings = g.Count(),
                Sellers = g.Select(x => x.FarmerId).Distinct().Count(),
                Avg = Math.Round(g.Average(x => x.Q!.Value)),
                Min = Math.Round(g.Min(x => x.Q!.Value)),
                Max = Math.Round(g.Max(x => x.Q!.Value)),
                TrendPercent = TrendPercent(points, g.Key, now)
            })
            .OrderByDescending(c => c.Listings)
            .ThenBy(c => c.Commodity)
            .ToList();
    }

    // Average price this month and the 5 months before it (0 where there is no data).
    public static List<MonthBar> Trend(List<PricePoint> points, string key, DateTime now)
    {
        var start = new DateTime(now.Year, now.Month, 1).AddMonths(-5);

        return Enumerable.Range(0, 6)
            .Select(i => start.AddMonths(i))
            .Select(m =>
            {
                var values = points
                    .Where(p => p.Key == key && p.Date.Year == m.Year && p.Date.Month == m.Month)
                    .Select(p => p.PerQuintal)
                    .ToList();

                return new MonthBar { Label = m.ToString("MMM"), Value = values.Count == 0 ? 0 : Math.Round(values.Average()) };
            })
            .ToList();
    }

    // Last 30 days against the 30 days before. Null until both periods have data.
    public static decimal? TrendPercent(List<PricePoint> points, string key, DateTime now)
    {
        var recent = points.Where(p => p.Key == key && p.Date > now.AddDays(-30)).Select(p => p.PerQuintal).ToList();
        var earlier = points.Where(p => p.Key == key && p.Date <= now.AddDays(-30) && p.Date > now.AddDays(-60)).Select(p => p.PerQuintal).ToList();
        if (recent.Count == 0 || earlier.Count == 0) return null;

        var before = earlier.Average();
        return before == 0 ? null : Math.Round((recent.Average() - before) / before * 100, 1);
    }

    public static (string Level, string Text) Advice(PriceRow row)
    {
        var gap = Math.Abs(row.DiffPercent ?? 0);

        return row.Verdict switch
        {
            "Above market" => ("warning",
                $"Your price is about {gap:0.#}% above the market average. Buyers may choose cheaper sellers. Consider a small reduction, or stress quality, freshness and harvest date in the description."),
            "Below market" => ("info",
                $"Your price is about {gap:0.#}% below the market average. You may be underpricing. If this product sells out quickly, raise the price in small steps toward the average."),
            "In line" => ("success",
                "Your price is in line with the market. Compete on quality, freshness and reliable delivery."),
            "No other sellers" => ("info",
                "You are the only seller of this product right now, so there is nothing to compare against yet."),
            _ => ("info",
                "This product is priced per dozen or piece, so it can't be compared per quintal yet.")
        };
    }
}
