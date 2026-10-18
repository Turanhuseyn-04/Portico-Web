using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Web.Controllers;
using Portico.Web.Helpers;
using Portico.Web.Models.Accounts;
using Portico.Web.Services;
using Portico.Web.ViewModels.Platform;

namespace Portico.Web.Areas.Admin.Controllers;

// SuperAdmin pages. The base class already requires staff; these also require the SuperAdmin role,
// and Portico.Api checks the same role again on every call they make.

[Authorize(Policy = AccountPolicies.SuperAdmin)]
public class AccountsController : ApiControllerBase
{
    public AccountsController(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        Page("Accounts", "Every account, its role, and who may run the houses.", "#accountsDesk", "Skip to accounts", "Accounts");
        var (accounts, errors, _) = await GetAsync<List<AccountRowVm>>("/admin/accounts");
        var query = q ?? "";
        return View(new AccountIndexVm
        {
            Query = query,
            Me = User.Email() ?? "",
            Items = (accounts ?? new List<AccountRowVm>())
                .Where(row => string.IsNullOrWhiteSpace(query) || AdminText.Has(row.Name, query) || AdminText.Has(row.Email, query) || AdminText.Has(row.Role, query))
                .ToList(),
            LoadError = accounts is null ? FirstError(errors, UnavailableMessage) : null
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Role(string email, string role)
    {
        if (string.IsNullOrWhiteSpace(email) || !AccountRoles.All.Contains(role))
        {
            TempData["Error"] = "Choose an account and one of the roles.";
            return RedirectToAction(nameof(Index));
        }

        var (success, errors, _) = await PutAsync($"/admin/accounts/{Uri.EscapeDataString(email)}/role", new { role });
        if (success) TempData["Success"] = $"{email} is now {role}.";
        else TempData["Error"] = FirstError(errors, "The role could not be changed.");
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Policy = AccountPolicies.SuperAdmin)]
public class SettingsController : ApiControllerBase
{
    private readonly ISettingsApi _settings;

    public SettingsController(IHttpClientFactory httpClientFactory, ISettingsApi settings) : base(httpClientFactory)
    {
        _settings = settings;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Page("Settings", "The contact details every page shows in its footer and on Help.", "#settingsForm", "Skip to settings", "Settings");
        var (values, errors, _) = await GetAsync<Dictionary<string, string>>("/settings");
        if (values is null) ViewData["LoadError"] = FirstError(errors, UnavailableMessage);
        return View(SettingsFormVm.From(values ?? new Dictionary<string, string>()));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Index(SettingsFormVm model)
    {
        Page("Settings", "The contact details every page shows in its footer and on Help.", "#settingsForm", "Skip to settings", "Settings");
        if (!ModelState.IsValid) return View(model);

        var (success, errors, _) = await PutAsync("/admin/settings", model.ToWrite());
        if (!success)
        {
            NoteErrors(errors);
            return View(model);
        }

        _settings.Forget();
        TempData["Success"] = "Settings saved. The footer shows them now.";
        return RedirectToAction(nameof(Index));
    }
}

[Authorize(Policy = AccountPolicies.SuperAdmin)]
public class AuditController : ApiControllerBase
{
    public AuditController(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        Page("Audit log", "Every change staff made, newest first, including the ones that were refused.", "#auditDesk", "Skip to the audit log", "Audit");
        var (entries, errors, _) = await GetAsync<List<AuditEntryVm>>("/admin/audit?take=200");
        return View(new AuditIndexVm
        {
            Items = entries ?? new List<AuditEntryVm>(),
            LoadError = entries is null ? FirstError(errors, UnavailableMessage) : null
        });
    }
}
