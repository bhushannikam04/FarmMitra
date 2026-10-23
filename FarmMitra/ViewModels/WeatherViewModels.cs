namespace FarmMitra.ViewModels;

public class GeoPlace
{
    public string Name { get; set; } = "";
    public string? Admin1 { get; set; }        // state / region
    public string? Country { get; set; }
    public string? CountryCode { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }

    public string Label
    {
        get
        {
            var parts = new List<string> { Name };
            if (!string.IsNullOrWhiteSpace(Admin1)) parts.Add(Admin1!);
            if (CountryCode != "IN" && !string.IsNullOrWhiteSpace(Country)) parts.Add(Country!);
            return string.Join(", ", parts);
        }
    }
}

public class CurrentWeather
{
    public double Temperature { get; set; }
    public double FeelsLike { get; set; }
    public double Humidity { get; set; }
    public double Rain { get; set; }
    public double WindSpeed { get; set; }
    public int WindDirection { get; set; }
    public int Code { get; set; }
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "fa-cloud";

    public string WindCompass
    {
        get
        {
            var names = new[] { "N", "NE", "E", "SE", "S", "SW", "W", "NW" };
            return names[(int)Math.Round(((WindDirection % 360) + 360) % 360 / 45.0) % 8];
        }
    }
}

public class DayForecast
{
    public DateOnly Date { get; set; }
    public string Label { get; set; } = "";     // Today, Tomorrow, Thu...
    public int Code { get; set; }
    public string Description { get; set; } = "";
    public string Icon { get; set; } = "fa-cloud";
    public double Max { get; set; }
    public double Min { get; set; }
    public double RainMm { get; set; }
    public double RainChance { get; set; }
    public double WindMax { get; set; }
    public double Uv { get; set; }
    public double Evaporation { get; set; }     // ET0, mm per day
}

public class WeatherReport
{
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime FetchedAtUtc { get; set; } = DateTime.UtcNow;
    public CurrentWeather Current { get; set; } = new();
    public List<DayForecast> Days { get; set; } = new();
}

public class Advisory
{
    public string Level { get; set; } = "info";   // danger, warning, info, success
    public string Icon { get; set; } = "fa-circle-info";
    public string Title { get; set; } = "";
    public string Text { get; set; } = "";
}

public class WeatherPageViewModel
{
    public string? Query { get; set; }
    public GeoPlace? Place { get; set; }
    public List<GeoPlace> Alternatives { get; set; } = new();
    public WeatherReport? Report { get; set; }
    public List<Advisory> Advisories { get; set; } = new();
    public string? Error { get; set; }
    public bool NeedsLocation { get; set; }
}
