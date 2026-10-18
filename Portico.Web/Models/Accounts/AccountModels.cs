using System.Globalization;
using System.Security.Claims;

namespace Portico.Web.Models.Accounts;

// The account Portico.Api returns after register and sign-in, with the token to send on the guest's behalf.
public sealed record AccountProfile(string Name, string Email, string Role, string? Token = null, DateTime? ExpiresAt = null);

// What register returns: the account waits for its email to be confirmed before it can sign in.
public sealed record Registered(string Name, string Email, bool ConfirmationRequired, bool EmailSent);

public static class AccountRoles
{
    public const string Guest = "Guest";
    public const string Admin = "Admin";
    public const string SuperAdmin = "SuperAdmin";

    public static readonly IReadOnlyList<string> All = new[] { Guest, Admin, SuperAdmin };
}

// Policy names used with [Authorize(Policy = ...)].
public static class AccountPolicies
{
    // Admin or SuperAdmin: rooms, reservations, reviews, photos, slides, videos.
    public const string Staff = "Staff";

    // SuperAdmin only: accounts, roles, settings, audit log.
    public const string SuperAdmin = "SuperAdmin";
}

// Reading the signed-in guest from the cookie the Web sets at sign-in.
public static class AccountClaims
{
    public const string ApiToken = "portico:api-token";
    public const string ApiExpires = "portico:api-expires";
    public const string CheckedAt = "portico:checked-at";

    public static string? Email(this ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true ? user.FindFirstValue(ClaimTypes.Email) : null;

    public static string DisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name) ?? user.Email() ?? "";

    public static string Role(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Role) ?? AccountRoles.Guest;

    public static bool IsStaff(this ClaimsPrincipal user) =>
        user.IsInRole(AccountRoles.Admin) || user.IsInRole(AccountRoles.SuperAdmin);

    public static bool IsSuperAdmin(this ClaimsPrincipal user) => user.IsInRole(AccountRoles.SuperAdmin);

    public static string? ApiTokenOf(this ClaimsPrincipal user) => user.FindFirstValue(ApiToken);

    public static DateTime? ApiExpiresOf(this ClaimsPrincipal user) =>
        DateTime.TryParse(user.FindFirstValue(ApiExpires), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at) ? at : null;

    public static DateTime? CheckedAtOf(this ClaimsPrincipal user) =>
        DateTime.TryParse(user.FindFirstValue(CheckedAt), CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var at) ? at : null;

    public static ClaimsPrincipal ToPrincipal(this AccountProfile profile, string scheme)
    {
        var claims = new List<Claim>
        {
            new(ClaimTypes.Name, profile.Name),
            new(ClaimTypes.Email, profile.Email),
            new(ClaimTypes.Role, profile.Role),
            new(CheckedAt, DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture))
        };
        if (profile.Token is not null) claims.Add(new Claim(ApiToken, profile.Token));
        if (profile.ExpiresAt is { } expires) claims.Add(new Claim(ApiExpires, expires.ToUniversalTime().ToString("O", CultureInfo.InvariantCulture)));
        return new ClaimsPrincipal(new ClaimsIdentity(claims, scheme));
    }
}
