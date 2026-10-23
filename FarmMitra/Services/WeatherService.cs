using System.Globalization;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FarmMitra.ViewModels;
using Microsoft.Extensions.Caching.Memory;

namespace FarmMitra.Services;

// Live weather from Open-Meteo.com (free, no API key). Results are cached so the page stays fast
// and we don't hammer the free service: places for 24 hours, forecasts for 15 minutes.
public class WeatherService
{
    private static readonly TimeSpan ForecastTtl = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan GeoTtl = TimeSpan.FromHours(24);

    private readonly HttpClient _http;
    private readonly IMemoryCache _cache;

    public WeatherService(HttpClient http, IMemoryCache cache)
    {
        _http = http;
        _cache = cache;
    }

    public async Task<List<GeoPlace>> SearchPlacesAsync(string name, CancellationToken ct = default)
    {
        name = name.Trim();
        if (name.Length < 2 || name.Length > 80) return new();

        var key = "geo:" + name.ToLowerInvariant();
        if (_cache.TryGetValue(key, out List<GeoPlace>? cached) && cached != null) return cached;

        var url = "https://geocoding-api.open-meteo.com/v1/search?name=" + Uri.EscapeDataString(name) +
                  "&count=10&language=en&format=json";

        var dto = await _http.GetFromJsonAsync<GeoResponse>(url, ct);

        var places = (dto?.Results ?? new List<GeoResult>())
            .Select(r => new GeoPlace
            {
                Name = r.Name ?? name,
                Admin1 = r.Admin1,
                Country = r.Country,
                CountryCode = r.CountryCode,
                Latitude = r.Latitude,
                Longitude = r.Longitude
            })
            .ToList();

        _cache.Set(key, places, GeoTtl);
        return places;
    }

    public async Task<WeatherReport> GetForecastAsync(double latitude, double longitude, CancellationToken ct = default)
    {
        var lat = Math.Round(latitude, 2);
        var lon = Math.Round(longitude, 2);
        var key = $"wx:{lat.ToString(CultureInfo.InvariantCulture)}:{lon.ToString(CultureInfo.InvariantCulture)}";

        if (_cache.TryGetValue(key, out WeatherReport? cached) && cached != null) return cached;

        var url = "https://api.open-meteo.com/v1/forecast" +
                  $"?latitude={lat.ToString(CultureInfo.InvariantCulture)}&longitude={lon.ToString(CultureInfo.InvariantCulture)}" +
                  "&current=temperature_2m,relative_humidity_2m,apparent_temperature,precipitation,weather_code,wind_speed_10m,wind_direction_10m" +
                  "&daily=weather_code,temperature_2m_max,temperature_2m_min,precipitation_sum,precipitation_probability_max,wind_speed_10m_max,uv_index_max,et0_fao_evapotranspiration" +
                  "&timezone=auto&forecast_days=7";

        var dto = await _http.GetFromJsonAsync<ForecastResponse>(url, ct);
        if (dto?.Current == null || dto.Daily?.Time == null)
            throw new HttpRequestException("Unexpected response from the weather service.");

        var c = dto.Current;
        var (cDesc, cIcon) = WeatherCodes.Describe(c.WeatherCode ?? 0);

        var report = new WeatherReport
        {
            Latitude = lat,
            Longitude = lon,
            FetchedAtUtc = DateTime.UtcNow,
            Current = new CurrentWeather
            {
                Temperature = c.Temperature ?? 0,
                FeelsLike = c.ApparentTemperature ?? c.Temperature ?? 0,
                Humidity = c.Humidity ?? 0,
                Rain = c.Precipitation ?? 0,
                WindSpeed = c.WindSpeed ?? 0,
                WindDirection = (int)Math.Round(c.WindDirection ?? 0),
                Code = c.WeatherCode ?? 0,
                Description = cDesc,
                Icon = cIcon
            }
        };

        var d = dto.Daily;
        for (var i = 0; i < d.Time.Count; i++)
        {
            var date = DateOnly.Parse(d.Time[i], CultureInfo.InvariantCulture);
            var code = d.WeatherCode != null && i < d.WeatherCode.Count ? d.WeatherCode[i] ?? 0 : 0;
            var (desc, icon) = WeatherCodes.Describe(code);

            report.Days.Add(new DayForecast
            {
                Date = date,
                Label = i == 0 ? "Today" : i == 1 ? "Tomorrow" : date.ToString("ddd", CultureInfo.InvariantCulture),
                Code = code,
                Description = desc,
                Icon = icon,
                Max = At(d.TemperatureMax, i),
                Min = At(d.TemperatureMin, i),
                RainMm = At(d.RainSum, i),
                RainChance = At(d.RainChance, i),
                WindMax = At(d.WindMax, i),
                Uv = At(d.UvMax, i),
                Evaporation = At(d.Et0, i)
            });
        }

        _cache.Set(key, report, ForecastTtl);
        return report;
    }

    private static double At(List<double?>? list, int i) =>
        list != null && i < list.Count ? list[i] ?? 0 : 0;

    // ---- JSON shapes returned by Open-Meteo ----
    private class GeoResponse
    {
        [JsonPropertyName("results")] public List<GeoResult>? Results { get; set; }
    }

    private class GeoResult
    {
        [JsonPropertyName("name")] public string? Name { get; set; }
        [JsonPropertyName("admin1")] public string? Admin1 { get; set; }
        [JsonPropertyName("country")] public string? Country { get; set; }
        [JsonPropertyName("country_code")] public string? CountryCode { get; set; }
        [JsonPropertyName("latitude")] public double Latitude { get; set; }
        [JsonPropertyName("longitude")] public double Longitude { get; set; }
    }

    private class ForecastResponse
    {
        [JsonPropertyName("current")] public CurrentDto? Current { get; set; }
        [JsonPropertyName("daily")] public DailyDto? Daily { get; set; }
    }

    private class CurrentDto
    {
        [JsonPropertyName("temperature_2m")] public double? Temperature { get; set; }
        [JsonPropertyName("relative_humidity_2m")] public double? Humidity { get; set; }
        [JsonPropertyName("apparent_temperature")] public double? ApparentTemperature { get; set; }
        [JsonPropertyName("precipitation")] public double? Precipitation { get; set; }
        [JsonPropertyName("weather_code")] public int? WeatherCode { get; set; }
        [JsonPropertyName("wind_speed_10m")] public double? WindSpeed { get; set; }
        [JsonPropertyName("wind_direction_10m")] public double? WindDirection { get; set; }
    }

    private class DailyDto
    {
        [JsonPropertyName("time")] public List<string>? Time { get; set; }
        [JsonPropertyName("weather_code")] public List<int?>? WeatherCode { get; set; }
        [JsonPropertyName("temperature_2m_max")] public List<double?>? TemperatureMax { get; set; }
        [JsonPropertyName("temperature_2m_min")] public List<double?>? TemperatureMin { get; set; }
        [JsonPropertyName("precipitation_sum")] public List<double?>? RainSum { get; set; }
        [JsonPropertyName("precipitation_probability_max")] public List<double?>? RainChance { get; set; }
        [JsonPropertyName("wind_speed_10m_max")] public List<double?>? WindMax { get; set; }
        [JsonPropertyName("uv_index_max")] public List<double?>? UvMax { get; set; }
        [JsonPropertyName("et0_fao_evapotranspiration")] public List<double?>? Et0 { get; set; }
    }
}

// WMO weather codes -> plain words + a Font Awesome icon.
public static class WeatherCodes
{
    public static (string Text, string Icon) Describe(int code) => code switch
    {
        0 => ("Clear sky", "fa-sun"),
        1 => ("Mainly clear", "fa-sun"),
        2 => ("Partly cloudy", "fa-cloud-sun"),
        3 => ("Overcast", "fa-cloud"),
        45 or 48 => ("Foggy", "fa-smog"),
        51 or 53 or 55 => ("Drizzle", "fa-cloud-rain"),
        56 or 57 => ("Freezing drizzle", "fa-cloud-rain"),
        61 => ("Light rain", "fa-cloud-rain"),
        63 => ("Rain", "fa-cloud-showers-heavy"),
        65 => ("Heavy rain", "fa-cloud-showers-heavy"),
        66 or 67 => ("Freezing rain", "fa-cloud-showers-heavy"),
        71 or 73 or 75 or 77 => ("Snow", "fa-snowflake"),
        80 => ("Light showers", "fa-cloud-sun-rain"),
        81 => ("Showers", "fa-cloud-showers-heavy"),
        82 => ("Violent showers", "fa-cloud-showers-heavy"),
        85 or 86 => ("Snow showers", "fa-snowflake"),
        95 => ("Thunderstorm", "fa-bolt"),
        96 or 99 => ("Thunderstorm with hail", "fa-bolt"),
        _ => ("Unsettled", "fa-cloud")
    };
}

// Turns forecast numbers into plain-language farming tips. These are simple rules of thumb,
// not an official warning: farmers should still follow IMD / local advisories.
public static class FarmAdvice
{
    public static List<Advisory> Build(WeatherReport report)
    {
        var list = new List<Advisory>();
        var next = report.Days.Take(5).ToList();
        if (next.Count == 0) return list;

        static string When(IEnumerable<DayForecast> days)
        {
            var names = days.Select(d => d.Label == "Today" ? "today" : d.Label == "Tomorrow" ? "tomorrow" : "on " + d.Label).ToList();
            return names.Count == 0 ? "" : string.Join(", ", names);
        }

        // Rain
        var heavy = next.Where(d => d.RainMm >= 50).ToList();
        var rainy = next.Where(d => d.RainMm >= 10 || d.RainChance >= 70).ToList();
        if (heavy.Any())
            list.Add(new Advisory
            {
                Level = "danger", Icon = "fa-cloud-showers-heavy",
                Title = $"Heavy rain forecast {When(heavy)}",
                Text = $"Up to about {heavy.Max(d => d.RainMm):0} mm expected. Clear field drainage, delay spraying and harvest, and keep harvested produce under cover."
            });
        else if (rainy.Any())
            list.Add(new Advisory
            {
                Level = "warning", Icon = "fa-cloud-rain",
                Title = $"Rain likely {When(rainy)}",
                Text = "Postpone pesticide or fertiliser application, and plan harvest and drying around it."
            });

        // Heat
        var hot = next.Where(d => d.Max >= 38).ToList();
        if (hot.Any())
        {
            var peak = hot.Max(d => d.Max);
            list.Add(new Advisory
            {
                Level = peak >= 42 ? "danger" : "warning", Icon = "fa-temperature-high",
                Title = $"Very hot {When(hot)} (up to {peak:0}°C)",
                Text = "Irrigate early morning or evening, avoid midday field work, and protect young plants from heat stress."
            });
        }

        // Cold
        var cold = next.Where(d => d.Min <= 8).ToList();
        if (cold.Any())
        {
            var low = cold.Min(d => d.Min);
            list.Add(new Advisory
            {
                Level = low <= 4 ? "danger" : "warning", Icon = "fa-temperature-low",
                Title = $"Cold nights {When(cold)} (down to {low:0}°C)",
                Text = "Cold-sensitive crops may be affected. Consider light irrigation in the evening or covering seedlings."
            });
        }

        // Wind
        var windy = next.Where(d => d.WindMax >= 25).ToList();
        if (windy.Any())
            list.Add(new Advisory
            {
                Level = "warning", Icon = "fa-wind",
                Title = $"Strong winds {When(windy)} (up to {windy.Max(d => d.WindMax):0} km/h)",
                Text = "Avoid spraying because the drift wastes product. Support tall or fruiting crops if needed."
            });

        // Dry spell and evaporation
        if (next.All(d => d.RainMm < 1 && d.RainChance < 30))
            list.Add(new Advisory
            {
                Level = "info", Icon = "fa-sun",
                Title = "Dry spell ahead",
                Text = "No meaningful rain is expected for the next few days. Plan irrigation, and use the dry days for harvest and drying."
            });

        if (next.Any(d => d.Evaporation >= 6))
            list.Add(new Advisory
            {
                Level = "info", Icon = "fa-droplet",
                Title = "High evaporation",
                Text = "Fields will lose water quickly on some days. Check soil moisture and consider mulching."
            });

        // Good spraying days
        var sprayDays = next.Take(3).Where(d => d.WindMax < 15 && d.RainChance < 20 && d.RainMm < 1).ToList();
        if (sprayDays.Any())
            list.Add(new Advisory
            {
                Level = "success", Icon = "fa-spray-can-sparkles",
                Title = $"Good conditions for spraying {When(sprayDays)}",
                Text = "Calm and dry: ideal for pesticide or fertiliser application."
            });

        if (list.Count == 0)
            list.Add(new Advisory
            {
                Level = "success", Icon = "fa-circle-check",
                Title = "No weather risks in the next few days",
                Text = "Conditions look normal for routine field work."
            });

        var order = new[] { "danger", "warning", "info", "success" };
        return list.OrderBy(a => Array.IndexOf(order, a.Level)).Take(5).ToList();
    }
}
