using System.Net.Http.Headers;
using Portico.Web.Models.Accounts;

namespace Portico.Web.Services;

// Sends the signed-in guest's API token with every call to Portico.Api. Calls made while nobody is signed in go without one.
public class ApiTokenHandler : DelegatingHandler
{
    private readonly IHttpContextAccessor _context;

    public ApiTokenHandler(IHttpContextAccessor context)
    {
        _context = context;
    }

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.Headers.Authorization is null && _context.HttpContext?.User.ApiTokenOf() is { } token)
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        return base.SendAsync(request, cancellationToken);
    }
}
