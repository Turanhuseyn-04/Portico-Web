using Microsoft.Extensions.Caching.Memory;

namespace Portico.Web.Services;

// The site settings every page shows in the footer, kept for a minute so pages do not ask the API each time.
public interface ISettingsApi
{
    Task<SiteSettings> GetAsync(CancellationToken cancellationToken);
    void Forget();
}

public sealed record SiteSettings(string SupportPhone, string SupportEmail, string SupportLocation)
{
    public static readonly SiteSettings Defaults = new("+994 55 299 28 03", "support@portico.az", "Baku, Azerbaijan");

    // The phone number as a tel: link, digits and the leading plus only.
    public string PhoneHref => "tel:" + new string(SupportPhone.Where(ch => char.IsAsciiDigit(ch) || ch == '+').ToArray());
}

public class SettingsApi : PorticoApiClient, ISettingsApi
{
    private const string CacheKey = "portico:settings";

    private readonly IMemoryCache _cache;

    public SettingsApi(HttpClient http, ILogger<SettingsApi> logger, IMemoryCache cache) : base(http, logger)
    {
        _cache = cache;
    }

    public async Task<SiteSettings> GetAsync(CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(CacheKey, out SiteSettings? cached) && cached is not null) return cached;

        var result = await GetJsonAsync<Dictionary<string, string>>("/settings", cancellationToken);
        if (result.Data is null) return SiteSettings.Defaults;

        var values = result.Data;
        var settings = new SiteSettings(
            values.GetValueOrDefault("SupportPhone") ?? SiteSettings.Defaults.SupportPhone,
            values.GetValueOrDefault("SupportEmail") ?? SiteSettings.Defaults.SupportEmail,
            values.GetValueOrDefault("SupportLocation") ?? SiteSettings.Defaults.SupportLocation);
        _cache.Set(CacheKey, settings, TimeSpan.FromMinutes(1));
        return settings;
    }

    public void Forget() => _cache.Remove(CacheKey);
}
