using System.Text.Json;
using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.Services;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace FarmMitra.Areas.Farmer.Controllers;

public class WeatherController : FarmerBaseController
{
    private readonly WeatherService _weather;
    private readonly ILogger<WeatherController> _log;

    public WeatherController(ApplicationDbContext db, UserManager<ApplicationUser> users,
        WeatherService weather, ILogger<WeatherController> log) : base(db, users)
    {
        _weather = weather;
        _log = log;
    }

    // Full page: current conditions, farming advice and a 7-day forecast.
    public async Task<IActionResult> Index(string? city, double? lat, double? lon, string? name, CancellationToken ct)
    {
        var query = !string.IsNullOrWhiteSpace(city) ? city.Trim() : DefaultCity();
        var vm = new WeatherPageViewModel { Query = query, NeedsLocation = query == null };

        try
        {
            var places = string.IsNullOrEmpty(query) ? new List<GeoPlace>() : await _weather.SearchPlacesAsync(query, ct);

            GeoPlace? place;
            if (lat is >= -90 and <= 90 && lon is >= -180 and <= 180)
            {
                // The farmer picked one of the "other matches".
                place = new GeoPlace
                {
                    Name = string.IsNullOrWhiteSpace(name) ? (query ?? "Selected location") : name.Trim(),
                    Latitude = lat.Value,
                    Longitude = lon.Value
                };
            }
            else
            {
                place = places.FirstOrDefault(p => p.CountryCode == "IN") ?? places.FirstOrDefault();
            }

            if (place == null)
            {
                if (query != null) vm.Error = $"We couldn't find \"{query}\". Check the spelling or try a nearby town.";
                return View(vm);
            }

            vm.Alternatives = places
                .Where(p => Math.Abs(p.Latitude - place.Latitude) > 0.001 || Math.Abs(p.Longitude - place.Longitude) > 0.001)
                .Take(4)
                .ToList();

            vm.Place = place;
            vm.NeedsLocation = false;
            vm.Report = await _weather.GetForecastAsync(place.Latitude, place.Longitude, ct);
            vm.Advisories = FarmAdvice.Build(vm.Report);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _log.LogWarning(ex, "Weather lookup failed");
            vm.Error = "Live weather isn't reachable right now. Check your internet connection and try again.";
            #if DEBUG
            vm.Error += $" [{ex.GetType().Name}: {ex.Message}" + (ex.InnerException != null ? " -> " + ex.InnerException.Message : "") + "]";
            #endif
        }

        return View(vm);
    }

    // Small JSON summary used by the dashboard card, so the dashboard itself never waits on weather.
    [HttpGet]
    public async Task<IActionResult> Summary(CancellationToken ct)
    {
        var query = DefaultCity();
        if (query == null)
            return Json(new { ok = false, message = "Open the Weather page and search for your town to see local weather." });

        try
        {
            var places = await _weather.SearchPlacesAsync(query, ct);
            var place = places.FirstOrDefault(p => p.CountryCode == "IN") ?? places.FirstOrDefault();
            if (place == null)
                return Json(new { ok = false, message = $"We couldn't find \"{query}\" on the weather map." });

            var report = await _weather.GetForecastAsync(place.Latitude, place.Longitude, ct);
            var today = report.Days.FirstOrDefault();
            var tip = FarmAdvice.Build(report).FirstOrDefault();

            return Json(new
            {
                ok = true,
                place = place.Label,
                temperature = Math.Round(report.Current.Temperature),
                description = report.Current.Description,
                icon = report.Current.Icon,
                humidity = Math.Round(report.Current.Humidity),
                rain = Math.Round(today?.RainMm ?? 0, 1),
                rainChance = Math.Round(today?.RainChance ?? 0),
                wind = Math.Round(report.Current.WindSpeed),
                high = Math.Round(today?.Max ?? 0),
                low = Math.Round(today?.Min ?? 0),
                tipTitle = tip?.Title,
                tipLevel = tip?.Level
            });
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException or JsonException)
        {
            _log.LogWarning(ex, "Weather summary failed");
            return Json(new { ok = false, message = "Weather is unavailable right now." });
        }
    }

    // City from the farmer's profile, falling back to the last part of the farm location.
    private string? DefaultCity()
    {
        if (!string.IsNullOrWhiteSpace(CurrentUser.City)) return CurrentUser.City.Trim();

        return CurrentUser.FarmLocation?
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .LastOrDefault();
    }
}
