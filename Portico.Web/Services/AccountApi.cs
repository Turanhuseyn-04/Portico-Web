using Portico.Web.Models.Accounts;

namespace Portico.Web.Services;

public interface IAccountApi
{
    Task<ApiResult<AccountProfile>> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken);
    Task<ApiResult<AccountProfile>> SignInAsync(string email, string password, CancellationToken cancellationToken);
    Task<ApiResult<AccountProfile>> ProfileAsync(string email, string token, CancellationToken cancellationToken);
    Task<ApiResult<List<string>>> SavedAsync(string email, CancellationToken cancellationToken);
    Task<ApiResult<List<string>>> SaveAsync(string email, string propertyId, CancellationToken cancellationToken);
    Task<ApiResult<List<string>>> UnsaveAsync(string email, string propertyId, CancellationToken cancellationToken);
}

public class AccountApi : PorticoApiClient, IAccountApi
{
    public AccountApi(HttpClient http, ILogger<AccountApi> logger) : base(http, logger)
    {
    }

    public Task<ApiResult<AccountProfile>> RegisterAsync(string name, string email, string password, CancellationToken cancellationToken) =>
        PostJsonAsync<AccountProfile>("/account/register", new { name, email, password }, cancellationToken);

    public Task<ApiResult<AccountProfile>> SignInAsync(string email, string password, CancellationToken cancellationToken) =>
        PostJsonAsync<AccountProfile>("/account/signin", new { email, password }, cancellationToken);

    public Task<ApiResult<AccountProfile>> ProfileAsync(string email, string token, CancellationToken cancellationToken) =>
        GetJsonAsync<AccountProfile>(AccountUrl(email), token, cancellationToken);

    public Task<ApiResult<List<string>>> SavedAsync(string email, CancellationToken cancellationToken) =>
        GetJsonAsync<List<string>>($"{AccountUrl(email)}/saved", cancellationToken);

    public Task<ApiResult<List<string>>> SaveAsync(string email, string propertyId, CancellationToken cancellationToken) =>
        PostJsonAsync<List<string>>($"{AccountUrl(email)}/saved/{Uri.EscapeDataString(propertyId)}", null, cancellationToken);

    public Task<ApiResult<List<string>>> UnsaveAsync(string email, string propertyId, CancellationToken cancellationToken) =>
        DeleteJsonAsync<List<string>>($"{AccountUrl(email)}/saved/{Uri.EscapeDataString(propertyId)}", cancellationToken);

    private static string AccountUrl(string email) => $"/account/{Uri.EscapeDataString(email)}";
}
