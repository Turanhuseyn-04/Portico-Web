using System.Net;
using System.Text.Json;
using Portico.Web.Areas.Admin.Controllers;

namespace Portico.Web.Controllers;

public abstract class ApiControllerBase : AdminControllerBase
{
    protected const string UnavailableMessage = "The service is unavailable right now. Please try again later.";

    private readonly IHttpClientFactory _httpClientFactory;

    protected ApiControllerBase(IHttpClientFactory httpClientFactory)
    {
        _httpClientFactory = httpClientFactory;
    }

    protected HttpClient Api => _httpClientFactory.CreateClient("PorticoApi");

    protected async Task<(T? Data, string[] Errors, int? Status)> GetAsync<T>(string url) where T : class
    {
        try
        {
            var response = await Api.GetAsync(url);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorsAsync(response), (int)response.StatusCode);

            if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
                return (null, Array.Empty<string>(), (int)response.StatusCode);

            var data = await response.Content.ReadFromJsonAsync<T>();
            return (data, Array.Empty<string>(), (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return (null, new[] { UnavailableMessage }, null);
        }
        catch (JsonException)
        {
            return (null, new[] { "Something went wrong. Please try again." }, null);
        }
    }

    protected async Task<(T? Data, string[] Errors, int? Status)> PostAsync<T>(string url, object body) where T : class
    {
        try
        {
            var response = await Api.PostAsJsonAsync(url, body);
            if (!response.IsSuccessStatusCode)
                return (null, await ReadErrorsAsync(response), (int)response.StatusCode);

            if (response.StatusCode == HttpStatusCode.NoContent || response.Content.Headers.ContentLength == 0)
                return (null, Array.Empty<string>(), (int)response.StatusCode);

            var data = await response.Content.ReadFromJsonAsync<T>();
            return (data, Array.Empty<string>(), (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return (null, new[] { UnavailableMessage }, null);
        }
        catch (JsonException)
        {
            return (null, new[] { "Something went wrong. Please try again." }, null);
        }
    }

    protected async Task<(bool Success, string[] Errors, int? Status)> PutAsync(string url, object body)
    {
        try
        {
            var response = await Api.PutAsJsonAsync(url, body);
            if (response.IsSuccessStatusCode)
                return (true, Array.Empty<string>(), (int)response.StatusCode);
            return (false, await ReadErrorsAsync(response), (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return (false, new[] { UnavailableMessage }, null);
        }
    }

    protected async Task<(bool Success, string[] Errors, int? Status)> DeleteAsync(string url)
    {
        try
        {
            var response = await Api.DeleteAsync(url);
            if (response.IsSuccessStatusCode)
                return (true, Array.Empty<string>(), (int)response.StatusCode);
            return (false, await ReadErrorsAsync(response), (int)response.StatusCode);
        }
        catch (Exception ex) when (ex is HttpRequestException or TaskCanceledException)
        {
            return (false, new[] { UnavailableMessage }, null);
        }
    }

    protected void NoteErrors(IEnumerable<string> errors)
    {
        foreach (var error in errors)
            ModelState.AddModelError(string.Empty, error);
    }

    protected static string FirstError(IEnumerable<string> errors, string fallback) =>
        errors.FirstOrDefault(text => !string.IsNullOrWhiteSpace(text)) ?? fallback;

    private static async Task<string[]> ReadErrorsAsync(HttpResponseMessage response)
    {
        var body = string.Empty;
        try
        {
            body = await response.Content.ReadAsStringAsync();
        }
        catch (HttpRequestException)
        {
            return new[] { Fallback(response.StatusCode) };
        }

        if (string.IsNullOrWhiteSpace(body))
            return new[] { Fallback(response.StatusCode) };

        try
        {
            using var document = JsonDocument.Parse(body);
            var root = document.RootElement;
            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("errors", out var errors))
            {
                var messages = new List<string>();
                if (errors.ValueKind == JsonValueKind.Array)
                {
                    foreach (var item in errors.EnumerateArray())
                    {
                        if (item.ValueKind == JsonValueKind.String && item.GetString() is string text && text.Length > 0)
                            messages.Add(text);
                    }
                }
                else if (errors.ValueKind == JsonValueKind.Object)
                {
                    foreach (var field in errors.EnumerateObject())
                    {
                        if (field.Value.ValueKind != JsonValueKind.Array) continue;
                        foreach (var item in field.Value.EnumerateArray())
                        {
                            if (item.ValueKind == JsonValueKind.String && item.GetString() is string text && text.Length > 0)
                                messages.Add(text);
                        }
                    }
                }

                if (messages.Count > 0) return messages.ToArray();
            }
        }
        catch (JsonException)
        {
        }

        return new[] { Fallback(response.StatusCode) };
    }

    private static string Fallback(HttpStatusCode status) => status switch
    {
        HttpStatusCode.NotFound => "That record does not exist.",
        HttpStatusCode.Conflict => "That change conflicts with an existing record.",
        _ => "Something went wrong. Please try again."
    };
}
