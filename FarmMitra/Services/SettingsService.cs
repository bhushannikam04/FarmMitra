using System.Globalization;
using FarmMitra.Data;
using FarmMitra.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;

namespace FarmMitra.Services;

public record SiteSettings(decimal FlatDeliveryFee, decimal FreeDeliveryAbove, string SupportEmail, string SupportPhone);

/// <summary>Reads and saves the platform settings the admin can change. Cached for 5 minutes.</summary>
public class SettingsService
{
    private const string CacheKey = "site-settings";
    private static readonly SiteSettings Defaults = new(200m, 10000m, "hello@farmmitra.in", "+91 12345 67890");

    private readonly ApplicationDbContext _db;
    private readonly IMemoryCache _cache;

    public SettingsService(ApplicationDbContext db, IMemoryCache cache)
    {
        _db = db;
        _cache = cache;
    }

    public async Task<SiteSettings> GetAsync()
    {
        if (_cache.TryGetValue(CacheKey, out SiteSettings? cached) && cached != null) return cached;

        try
        {
            var rows = await _db.PlatformSettings.AsNoTracking().ToDictionaryAsync(s => s.Key, s => s.Value);

            decimal Dec(string key, decimal fallback) =>
                rows.TryGetValue(key, out var v) && decimal.TryParse(v, NumberStyles.Number, CultureInfo.InvariantCulture, out var d) && d >= 0 ? d : fallback;

            string Str(string key, string fallback) =>
                rows.TryGetValue(key, out var v) && !string.IsNullOrWhiteSpace(v) ? v : fallback;

            var s = new SiteSettings(
                Dec("FlatDeliveryFee", Defaults.FlatDeliveryFee),
                Dec("FreeDeliveryAbove", Defaults.FreeDeliveryAbove),
                Str("SupportEmail", Defaults.SupportEmail),
                Str("SupportPhone", Defaults.SupportPhone));

            _cache.Set(CacheKey, s, TimeSpan.FromMinutes(5));
            return s;
        }
        catch
        {
            // Table not created yet (migration not run) - fall back to defaults instead of crashing every page.
            _cache.Set(CacheKey, Defaults, TimeSpan.FromSeconds(30));
            return Defaults;
        }
    }

    public async Task UpdateAsync(SiteSettings s)
    {
        var values = new Dictionary<string, string>
        {
            ["FlatDeliveryFee"] = s.FlatDeliveryFee.ToString(CultureInfo.InvariantCulture),
            ["FreeDeliveryAbove"] = s.FreeDeliveryAbove.ToString(CultureInfo.InvariantCulture),
            ["SupportEmail"] = s.SupportEmail.Trim(),
            ["SupportPhone"] = s.SupportPhone.Trim()
        };

        var existing = await _db.PlatformSettings.ToDictionaryAsync(x => x.Key);
        foreach (var (key, value) in values)
        {
            if (existing.TryGetValue(key, out var row)) row.Value = value;
            else _db.PlatformSettings.Add(new PlatformSetting { Key = key, Value = value });
        }

        await _db.SaveChangesAsync();
        _cache.Remove(CacheKey);
    }
}
