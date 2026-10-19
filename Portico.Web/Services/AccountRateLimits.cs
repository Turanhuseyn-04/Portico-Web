using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;

namespace Portico.Web.Services;

// Sign-in, register, and resend take at most a few tries a minute from one address. Portico.Api sees every guest
// as the Web's own address, so this is the only place a flood from one visitor can be told apart.
public static class AccountRateLimits
{
    public const string Policy = "account-forms";
    public const int PermitsPerMinute = 10;

    public static void Add(RateLimiterOptions options)
    {
        options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
        options.AddPolicy(Policy, context => RateLimitPartition.GetFixedWindowLimiter(
            context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
            _ => new FixedWindowRateLimiterOptions
            {
                PermitLimit = PermitsPerMinute,
                Window = TimeSpan.FromMinutes(1),
                QueueLimit = 0
            }));
        options.OnRejected = async (context, cancellationToken) =>
        {
            context.HttpContext.Response.ContentType = "text/plain; charset=utf-8";
            await context.HttpContext.Response.WriteAsync("Too many attempts from this address. Wait a minute, then go back and try again.", cancellationToken);
        };
    }
}
