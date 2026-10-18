using System.Security.Claims;

namespace Portico.Web.Models.Accounts;

// The account Portico.Api returns after register and sign-in.
public sealed record AccountProfile(string Name, string Email, string Role);

public static class AccountRoles
{
    public const string Admin = "Admin";
}

// Reading the signed-in guest from the cookie the Web sets at sign-in.
public static class AccountClaims
{
    public static string? Email(this ClaimsPrincipal user) =>
        user.Identity?.IsAuthenticated == true ? user.FindFirstValue(ClaimTypes.Email) : null;

    public static string DisplayName(this ClaimsPrincipal user) =>
        user.FindFirstValue(ClaimTypes.Name) ?? user.Email() ?? "";

    public static ClaimsPrincipal ToPrincipal(this AccountProfile profile, string scheme) =>
        new(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.Name, profile.Name),
            new Claim(ClaimTypes.Email, profile.Email),
            new Claim(ClaimTypes.Role, profile.Role)
        }, scheme));
}
