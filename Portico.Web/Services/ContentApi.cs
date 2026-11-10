using Microsoft.Extensions.Caching.Memory;
using Portico.Web.ViewModels.Slides;
using Portico.Web.ViewModels.Videos;

namespace Portico.Web.Services;

// The home page's trending destination slides and video band, as the admin arranged them in Portico.Api.
// Kept for a minute so the home page does not ask the API on every visit; the admin forgets them after a change.
public interface IContentApi
{
    Task<IReadOnlyList<SlideVm>?> SlidesAsync(CancellationToken cancellationToken);
    Task<IReadOnlyList<VideoVm>?> VideosAsync(CancellationToken cancellationToken);
    void Forget();
}

public class ContentApi : PorticoApiClient, IContentApi
{
    private const string SlidesKey = "portico:slides";
    private const string VideosKey = "portico:videos";

    private readonly IMemoryCache _cache;

    public ContentApi(HttpClient http, ILogger<ContentApi> logger, IMemoryCache cache) : base(http, logger)
    {
        _cache = cache;
    }

    // Null when the API could not be reached, so the page can tell "none" from "not available".
    public Task<IReadOnlyList<SlideVm>?> SlidesAsync(CancellationToken cancellationToken) =>
        ReadAsync<SlideVm>(SlidesKey, "/slides", cancellationToken);

    public Task<IReadOnlyList<VideoVm>?> VideosAsync(CancellationToken cancellationToken) =>
        ReadAsync<VideoVm>(VideosKey, "/videos", cancellationToken);

    public void Forget()
    {
        _cache.Remove(SlidesKey);
        _cache.Remove(VideosKey);
    }

    private async Task<IReadOnlyList<T>?> ReadAsync<T>(string key, string url, CancellationToken cancellationToken)
    {
        if (_cache.TryGetValue(key, out IReadOnlyList<T>? cached) && cached is not null) return cached;

        var result = await GetJsonAsync<List<T>>(url, cancellationToken);
        if (result.Data is null) return null;

        _cache.Set(key, (IReadOnlyList<T>)result.Data, TimeSpan.FromMinutes(1));
        return result.Data;
    }
}
