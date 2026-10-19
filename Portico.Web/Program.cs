using System.Net.Security;
using Microsoft.AspNetCore.Authentication.Cookies;
using Portico.Web.Models.Accounts;
using Portico.Web.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews(options =>
{
    options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;
});

builder.Services.AddHttpContextAccessor();
builder.Services.AddMemoryCache();
builder.Services.AddTransient<ApiTokenHandler>();

// The admin controllers use the named client; the guest pages use typed clients. Both talk to the same API
// and send the signed-in guest's token with every call.
UsePorticoApi(builder.Services.AddHttpClient("PorticoApi"));
UsePorticoApi(builder.Services.AddHttpClient<IPropertyApi, PropertyApi>());
UsePorticoApi(builder.Services.AddHttpClient<IReservationApi, ReservationApi>());
UsePorticoApi(builder.Services.AddHttpClient<IAccountApi, AccountApi>());
UsePorticoApi(builder.Services.AddHttpClient<ISettingsApi, SettingsApi>());

// Portico.Api checks the password and issues a token; the Web keeps the guest and that token in its own cookie.
// The cookie lasts as long as the token, and SessionCheck signs out early when the role changes or the account is gone.
builder.Services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(options =>
    {
        options.LoginPath = "/signin";
        options.LogoutPath = "/signout";
        options.AccessDeniedPath = "/denied";
        options.Cookie.Name = "portico-session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(7);
        options.SlidingExpiration = false;
        options.Events.OnValidatePrincipal = SessionCheck.ValidateAsync;
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy(AccountPolicies.Staff, policy => policy.RequireRole(AccountRoles.Admin, AccountRoles.SuperAdmin));
    options.AddPolicy(AccountPolicies.SuperAdmin, policy => policy.RequireRole(AccountRoles.SuperAdmin));
});

builder.Services.AddRateLimiter(AccountRateLimits.Add);

void UsePorticoApi(IHttpClientBuilder client)
{
    client.AddHttpMessageHandler<ApiTokenHandler>();
    client.ConfigureHttpClient(http =>
    {
        var baseUrl = builder.Configuration["ApiSettings:BaseUrl"];
        if (string.IsNullOrWhiteSpace(baseUrl))
            throw new InvalidOperationException("ApiSettings:BaseUrl is missing.");
        http.BaseAddress = new Uri(baseUrl);
    })
    .ConfigurePrimaryHttpMessageHandler(() =>
    {
        var handler = new HttpClientHandler();
        if (builder.Environment.IsDevelopment())
        {
            handler.ServerCertificateCustomValidationCallback = (request, _, _, errors) =>
                errors == SslPolicyErrors.None || request.RequestUri?.IsLoopback == true;
        }

        return handler;
    });
}

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Dashboard}/{action=Index}/{id?}");

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();
