using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Web.Models;
using Portico.Web.Models.Accounts;
using Portico.Web.Services;
using Portico.Web.ViewModels.Account;
using Portico.Web.ViewModels.Home;

namespace Portico.Web.Controllers
{
    public class AccountController : Controller
    {
        private const string Scheme = CookieAuthenticationDefaults.AuthenticationScheme;

        private readonly IAccountApi _accounts;
        private readonly IPropertyApi _properties;

        public AccountController(IAccountApi accounts, IPropertyApi properties)
        {
            _accounts = accounts;
            _properties = properties;
        }

        [HttpGet("/signin")]
        public IActionResult SignIn(string? email, string? returnUrl)
        {
            AccountPage("signin", false);
            return View("Index", new AccountPageVm { Mode = "signin", SignIn = new SignInForm { Email = email ?? "", ReturnUrl = returnUrl } });
        }

        [HttpGet("/signin/failed")]
        public IActionResult SignInFailed(string? email, string? returnUrl)
        {
            AccountPage("signin", true);
            return View("Index", new AccountPageVm { Mode = "signin", Failed = true, SignIn = new SignInForm { Email = email ?? "", ReturnUrl = returnUrl } });
        }

        [HttpPost("/signin")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignIn([Bind(Prefix = "SignIn")] SignInForm form, CancellationToken cancellationToken)
        {
            AccountPage("signin", false);
            var model = new AccountPageVm { Mode = "signin", SignIn = form };
            if (!ModelState.IsValid) return View("Index", model);

            var profile = await _accounts.SignInAsync(form.Email.Trim(), form.Password, cancellationToken);
            if (profile.Data is not null)
            {
                await SignInAsync(profile.Data);
                return LocalRedirect(SafeReturn(form.ReturnUrl, Url.Action(nameof(Profile))!));
            }

            if (profile.Status == StatusCodes.Status401Unauthorized)
                return RedirectToAction(nameof(SignInFailed), new { email = form.Email.Trim(), returnUrl = form.ReturnUrl });

            model.Error = "Sign-in is unavailable right now. Try again in a moment.";
            return View("Index", model);
        }

        [HttpGet("/register")]
        public IActionResult Register(string? returnUrl)
        {
            AccountPage("register", false);
            return View("Index", new AccountPageVm { Mode = "register", Register = new RegisterForm { ReturnUrl = returnUrl } });
        }

        [HttpPost("/register")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind(Prefix = "Register")] RegisterForm form, CancellationToken cancellationToken)
        {
            AccountPage("register", false);
            var model = new AccountPageVm { Mode = "register", Register = form };
            if (!ModelState.IsValid) return View("Index", model);

            var profile = await _accounts.RegisterAsync(form.Name.Trim(), form.Email.Trim(), form.Password, cancellationToken);
            if (profile.Data is null)
            {
                model.Error = profile.Status is StatusCodes.Status400BadRequest or StatusCodes.Status409Conflict
                    ? profile.FirstError ?? "That account could not be created."
                    : "Registration is unavailable right now. Try again in a moment.";
                return View("Index", model);
            }

            await SignInAsync(profile.Data);
            return LocalRedirect(SafeReturn(form.ReturnUrl, Url.Action(nameof(Profile))!));
        }

        [HttpPost("/signout")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SignOutNow()
        {
            await HttpContext.SignOutAsync(Scheme);
            return RedirectToAction("Index", "Home");
        }

        [HttpGet("/denied")]
        public IActionResult Denied()
        {
            ViewData["Title"] = "Admins only";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            Response.StatusCode = StatusCodes.Status403Forbidden;
            return View("Message", new PageMessage("account", "Account", "Admins only", "This page is for Portico admins. You are signed in as a guest.", Url.Action(nameof(Profile))!, "Your account"));
        }

        [Authorize]
        [HttpGet("/account")]
        public IActionResult Profile()
        {
            ViewData["Title"] = "Your account";
            ViewData["Description"] = "Your name and email, with links to your trips and saved stays.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#account";
            ViewData["SkipText"] = "Skip to your account";
            ViewData["Account"] = "profile";
            ViewData["FooterNote"] = "Portico.Api keeps the account. The password is stored only as a hash.";
            return View("Profile");
        }

        [Authorize]
        [HttpGet("/saved")]
        public async Task<IActionResult> Saved(CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Saved stays";
            ViewData["Description"] = "Stays you saved with the heart.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#saved";
            ViewData["SkipText"] = "Skip to saved stays";
            ViewData["FooterNote"] = "A heart on a stay keeps it here.";

            var saved = await _accounts.SavedAsync(User.Email()!, cancellationToken);
            var stays = await _properties.ListAsync(cancellationToken);
            if (saved.Data is null || stays.Data is null)
                return View("Message", new PageMessage("saved", "Account", "Saved stays did not load", "Portico.Api did not answer. Try again in a moment.", Request.Path, "Try again"));

            var cards = stays.Data.Select(StayCardVm.From).ToDictionary(card => card.Id);
            return View("Saved", saved.Data.Where(cards.ContainsKey).Select(id => cards[id]).ToList());
        }

        // The heart on a stay. It saves or removes, then goes back to the page it was pressed on.
        [Authorize]
        [HttpPost("/saved/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleSaved(string id, bool saved, string? returnUrl, CancellationToken cancellationToken)
        {
            var email = User.Email()!;
            var result = saved
                ? await _accounts.UnsaveAsync(email, id, cancellationToken)
                : await _accounts.SaveAsync(email, id, cancellationToken);
            if (result.Data is null) TempData["Toast"] = "That stay could not be saved. Try again.";
            return LocalRedirect(SafeReturn(returnUrl, Url.Action(nameof(Saved))!));
        }

        // The cookie ends when the API token does, so the Web never holds a session the API would refuse.
        private Task SignInAsync(AccountProfile profile) =>
            HttpContext.SignInAsync(Scheme, profile.ToPrincipal(Scheme), new AuthenticationProperties
            {
                IsPersistent = true,
                ExpiresUtc = profile.ExpiresAt is { } expires ? new DateTimeOffset(expires.ToUniversalTime()) : null
            });

        // Only addresses on this site, so a crafted link cannot send a guest elsewhere after sign-in.
        private string SafeReturn(string? returnUrl, string fallback) =>
            !string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl) ? returnUrl : fallback;

        private void AccountPage(string mode, bool failed)
        {
            var register = mode == "register";
            ViewData["Title"] = failed ? "Sign in failed" : register ? "Create an account" : "Sign in";
            ViewData["Description"] = register ? "Create a Portico account." : "Sign in to Portico.";
            ViewData["Account"] = register ? "register" : "signin";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#account";
            ViewData["SkipText"] = register ? "Skip to create an account" : "Skip to sign in";
            ViewData["FooterNote"] = "Portico.Api keeps the account. The password is stored only as a hash.";
        }
    }
}
