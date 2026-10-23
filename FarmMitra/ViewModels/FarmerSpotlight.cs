namespace FarmMitra.ViewModels;

/// <summary>A public farmer card for the homepage, with loyalty stats worked out from real orders.</summary>
public class FarmerSpotlight
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string? PhotoUrl { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public List<string> Crops { get; set; } = new();
    public int? ExperienceYears { get; set; }
    public DateTime JoinedAt { get; set; }
    public int ProductCount { get; set; }

    public int DeliveredOrders { get; set; }
    public int Buyers { get; set; }
    public int RepeatBuyers { get; set; }

    public string Initial => string.IsNullOrWhiteSpace(Name) ? "F" : Name.Trim().Substring(0, 1).ToUpper();
    public string Location => string.Join(", ", new[] { City, State }.Where(s => !string.IsNullOrWhiteSpace(s)));

    // Loyalty: every delivered order earns 10 points, every buyer who came back earns 25 more.
    public int Points => DeliveredOrders * 10 + RepeatBuyers * 25;
    public int RepeatRate => Buyers == 0 ? 0 : (int)Math.Round(100.0 * RepeatBuyers / Buyers);

    private static readonly (string Name, int From)[] Tiers =
    {
        ("Seedling", 0), ("Bronze", 50), ("Silver", 150), ("Gold", 400)
    };

    public string Tier => Tiers.Last(t => Points >= t.From).Name;
    public string TierClass => "tier-" + Tier.ToLower();

    public (string Name, int From)? Next
    {
        get
        {
            foreach (var t in Tiers)
                if (t.From > Points) return t;
            return null;
        }
    }

    /// <summary>Progress (0-100) from the current tier to the next one.</summary>
    public int ProgressPct
    {
        get
        {
            var current = Tiers.Last(t => Points >= t.From).From;
            if (Next is not { } n) return 100;
            return (int)Math.Clamp(100.0 * (Points - current) / (n.From - current), 0, 100);
        }
    }
}
