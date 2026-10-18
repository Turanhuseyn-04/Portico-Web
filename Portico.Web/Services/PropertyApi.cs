using System.Text.Json;
using Portico.Web.Models.Stays;

namespace Portico.Web.Services;

public sealed record ApiResult<T>(T? Data, int? Status) where T : class
{
    public bool IsNotFound => Status == StatusCodes.Status404NotFound;
}

public interface IPropertyApi
{
    Task<ApiResult<List<StayListItem>>> ListAsync(CancellationToken cancellationToken);
    Task<ApiResult<StayDetail>> GetAsync(string id, CancellationToken cancellationToken);
    Task<ApiResult<List<RoomAvailability>>> AvailabilityAsync(string id, DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken);
}

public class PropertyApi : IPropertyApi
{
    private readonly HttpClient _http;
    private readonly ILogger<PropertyApi> _logger;

    public PropertyApi(HttpClient http, ILogger<PropertyApi> logger)
    {
        _http = http;
        _logger = logger;
    }

    public Task<ApiResult<List<StayListItem>>> ListAsync(CancellationToken cancellationToken) =>
        GetJsonAsync<List<StayListItem>>("/properties", cancellationToken);

    public Task<ApiResult<StayDetail>> GetAsync(string id, CancellationToken cancellationToken) =>
        GetJsonAsync<StayDetail>($"/properties/{Uri.EscapeDataString(id)}", cancellationToken);

    public Task<ApiResult<List<RoomAvailability>>> AvailabilityAsync(string id, DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken) =>
        GetJsonAsync<List<RoomAvailability>>(
            $"/properties/{Uri.EscapeDataString(id)}/availability?checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}",
            cancellationToken);

    private async Task<ApiResult<T>> GetJsonAsync<T>(string url, CancellationToken cancellationToken) where T : class
    {
        try
        {
            using var response = await _http.GetAsync(url, cancellationToken);
            if (!response.IsSuccessStatusCode)
                return new ApiResult<T>(null, (int)response.StatusCode);

            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return new ApiResult<T>(data, (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(ex, "Portico.Api call to {Url} failed.", url);
            return new ApiResult<T>(null, null);
        }
    }
}
