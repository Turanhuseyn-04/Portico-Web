using System.Text.Json;
using Portico.Web.Models.Stays;

namespace Portico.Web.Services;

// Data, or the status the API answered with (null when it could not be reached), and the API's own messages.
public sealed record ApiResult<T>(T? Data, int? Status, IReadOnlyList<string> Errors) where T : class
{
    public bool IsNotFound => Status == StatusCodes.Status404NotFound;
    public bool IsConflict => Status == StatusCodes.Status409Conflict;
    public string? FirstError => Errors.FirstOrDefault(text => !string.IsNullOrWhiteSpace(text));
}

// Sends requests to Portico.Api and turns every outcome into an ApiResult, so pages never see an exception for a bad answer.
public abstract class PorticoApiClient
{
    private readonly HttpClient _http;
    private readonly ILogger _logger;

    protected PorticoApiClient(HttpClient http, ILogger logger)
    {
        _http = http;
        _logger = logger;
    }

    protected Task<ApiResult<T>> GetJsonAsync<T>(string url, CancellationToken cancellationToken) where T : class =>
        SendAsync<T>(() => _http.GetAsync(url, cancellationToken), url, cancellationToken);

    protected Task<ApiResult<T>> PostJsonAsync<T>(string url, object? body, CancellationToken cancellationToken) where T : class =>
        SendAsync<T>(() => _http.PostAsJsonAsync(url, body, cancellationToken), url, cancellationToken);

    protected Task<ApiResult<T>> DeleteJsonAsync<T>(string url, CancellationToken cancellationToken) where T : class =>
        SendAsync<T>(() => _http.DeleteAsync(url, cancellationToken), url, cancellationToken);

    // For calls made before the request knows who is signed in, such as checking a sign-in cookie.
    protected Task<ApiResult<T>> GetJsonAsync<T>(string url, string bearer, CancellationToken cancellationToken) where T : class =>
        SendAsync<T>(() =>
        {
            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new System.Net.Http.Headers.AuthenticationHeaderValue("Bearer", bearer);
            return _http.SendAsync(request, cancellationToken);
        }, url, cancellationToken);

    private async Task<ApiResult<T>> SendAsync<T>(Func<Task<HttpResponseMessage>> send, string url, CancellationToken cancellationToken) where T : class
    {
        try
        {
            using var response = await send();
            if (!response.IsSuccessStatusCode)
                return new ApiResult<T>(null, (int)response.StatusCode, await ReadErrorsAsync(response, cancellationToken));

            var data = await response.Content.ReadFromJsonAsync<T>(cancellationToken);
            return new ApiResult<T>(data, (int)response.StatusCode, Array.Empty<string>());
        }
        catch (Exception ex) when (ex is HttpRequestException or JsonException || (ex is TaskCanceledException && !cancellationToken.IsCancellationRequested))
        {
            _logger.LogWarning(ex, "Portico.Api call to {Url} failed.", url);
            return new ApiResult<T>(null, null, Array.Empty<string>());
        }
    }

    // The API answers errors as { "errors": ["..."] }.
    private static async Task<IReadOnlyList<string>> ReadErrorsAsync(HttpResponseMessage response, CancellationToken cancellationToken)
    {
        try
        {
            using var document = JsonDocument.Parse(await response.Content.ReadAsStringAsync(cancellationToken));
            if (document.RootElement.ValueKind == JsonValueKind.Object
                && document.RootElement.TryGetProperty("errors", out var errors)
                && errors.ValueKind == JsonValueKind.Array)
            {
                return errors.EnumerateArray()
                    .Where(item => item.ValueKind == JsonValueKind.String)
                    .Select(item => item.GetString()!)
                    .ToList();
            }
        }
        catch (JsonException)
        {
        }

        return Array.Empty<string>();
    }
}

public interface IPropertyApi
{
    Task<ApiResult<List<StayListItem>>> ListAsync(CancellationToken cancellationToken);
    Task<ApiResult<StayDetail>> GetAsync(string id, CancellationToken cancellationToken);
    Task<ApiResult<List<RoomAvailability>>> AvailabilityAsync(string id, DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken);
}

public class PropertyApi : PorticoApiClient, IPropertyApi
{
    public PropertyApi(HttpClient http, ILogger<PropertyApi> logger) : base(http, logger)
    {
    }

    public Task<ApiResult<List<StayListItem>>> ListAsync(CancellationToken cancellationToken) =>
        GetJsonAsync<List<StayListItem>>("/properties", cancellationToken);

    public Task<ApiResult<StayDetail>> GetAsync(string id, CancellationToken cancellationToken) =>
        GetJsonAsync<StayDetail>($"/properties/{Uri.EscapeDataString(id)}", cancellationToken);

    public Task<ApiResult<List<RoomAvailability>>> AvailabilityAsync(string id, DateOnly checkIn, DateOnly checkOut, CancellationToken cancellationToken) =>
        GetJsonAsync<List<RoomAvailability>>(
            $"/properties/{Uri.EscapeDataString(id)}/availability?checkIn={checkIn:yyyy-MM-dd}&checkOut={checkOut:yyyy-MM-dd}",
            cancellationToken);
}
