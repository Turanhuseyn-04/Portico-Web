using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using Portico.Web.Controllers;

namespace Portico.Web.Areas.Admin.Controllers;

// The admin console's scripts read reservations, messages, and availability from the browser. They call this
// same-site address instead of Portico.Api, so the API token stays in the server-side cookie. Only the paths below
// are passed on, a change needs the anti-forgery token, and the API still checks the caller's role.
public class ApiProxyController : ApiControllerBase
{
    private static readonly Regex Reads = new(@"^(reservations|properties|properties/[^/]+/availability|messages|account/[^/]+/saved)$", RegexOptions.Compiled);
    private static readonly Regex Writes = new(@"^reservations/[^/]+/cancel$", RegexOptions.Compiled);

    public ApiProxyController(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    [HttpGet("/Admin/api/{**path}")]
    public Task<IActionResult> Read(string path, CancellationToken cancellationToken) =>
        ForwardAsync(HttpMethod.Get, path, Reads, cancellationToken);

    [HttpPost("/Admin/api/{**path}")]
    [ValidateAntiForgeryToken]
    public Task<IActionResult> Write(string path, CancellationToken cancellationToken) =>
        ForwardAsync(HttpMethod.Post, path, Writes, cancellationToken);

    private async Task<IActionResult> ForwardAsync(HttpMethod method, string? path, Regex allowed, CancellationToken cancellationToken)
    {
        var segments = (path ?? "").Split('/');
        if (!allowed.IsMatch(path ?? "") || segments.Any(segment => segment is "" or "." or "..")) return NotFound();

        var target = "/" + string.Join("/", segments.Select(Uri.EscapeDataString)) + Request.QueryString;
        try
        {
            using var response = await Api.SendAsync(new HttpRequestMessage(method, target), cancellationToken);
            return new ContentResult
            {
                StatusCode = (int)response.StatusCode,
                Content = await response.Content.ReadAsStringAsync(cancellationToken),
                ContentType = response.Content.Headers.ContentType?.ToString() ?? "application/json"
            };
        }
        catch (HttpRequestException)
        {
            return StatusCode(StatusCodes.Status502BadGateway);
        }
    }
}
