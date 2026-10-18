using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Portico.Web.Models.Accounts;

namespace Portico.Web.Services;

// Keeps the sign-in cookie honest. The API already checks the role on every call; this keeps the Web's menus and
// [Authorize] checks in step with it, so a demoted admin loses the admin pages within a minute, not when the cookie ends.
public static class SessionCheck
{
    private static readonly TimeSpan Every = TimeSpan.FromMinutes(1);

    public static async Task ValidateAsync(CookieValidatePrincipalContext context)
    {
        var user = context.Principal;
        var token = user?.ApiTokenOf();
        var email = user?.Email();
        if (user is null || token is null || email is null || user.ApiExpiresOf() is not { } expires || expires <= DateTime.UtcNow)
        {
            await Reject(context);
            return;
        }

        if (user.CheckedAtOf() is { } checkedAt && DateTime.UtcNow - checkedAt < Every) return;

        var accounts = context.HttpContext.RequestServices.GetRequiredService<IAccountApi>();
        var profile = await accounts.ProfileAsync(email, token, context.HttpContext.RequestAborted);
        if (profile.Status is StatusCodes.Status401Unauthorized or StatusCodes.Status403Forbidden or StatusCodes.Status404NotFound)
        {
            await Reject(context);
            return;
        }

        // If the API cannot be reached, keep the current cookie and try again on a later request.
        if (profile.Data is null) return;

        context.ReplacePrincipal((profile.Data with { Token = token, ExpiresAt = expires }).ToPrincipal(context.Scheme.Name));
        context.ShouldRenew = true;
    }

    private static async Task Reject(CookieValidatePrincipalContext context)
    {
        context.RejectPrincipal();
        await context.HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
    }
}
