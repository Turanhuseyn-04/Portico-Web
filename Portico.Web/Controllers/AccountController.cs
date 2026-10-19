using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
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
        public IActionResult SignIn(string? email, string? returnUrl, bool confirmed = false)
        {
            AccountPage("signin", false);
            return View("Index", new AccountPageVm { Mode = "signin", Confirmed = confirmed, SignIn = new SignInForm { Email = email ?? "", ReturnUrl = returnUrl } });
        }

        // The password was right, but the email has not been confirmed yet.
        [HttpGet("/signin/unconfirmed")]
        public IActionResult SignInUnconfirmed(string? email, string? returnUrl)
        {
            AccountPage("signin", false);
            ViewData["Title"] = "Confirm your email";
            return View("Index", new AccountPageVm { Mode = "signin", Unconfirmed = true, SignIn = new SignInForm { Email = email ?? "", ReturnUrl = returnUrl } });
        }

        [HttpGet("/signin/failed")]
        public IActionResult SignInFailed(string? email, string? returnUrl)
        {
            AccountPage("signin", true);
            return View("Index", new AccountPageVm { Mode = "signin", Failed = true, SignIn = new SignInForm { Email = email ?? "", ReturnUrl = returnUrl } });
        }

        [HttpPost("/signin")]
        [EnableRateLimiting(AccountRateLimits.Policy)]
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

            if (profile.Status == StatusCodes.Status403Forbidden)
                return RedirectToAction(nameof(SignInUnconfirmed), new { email = form.Email.Trim(), returnUrl = form.ReturnUrl });

            model.Error = profile.Status == StatusCodes.Status429TooManyRequests
                ? profile.FirstError ?? "Too many wrong passwords. Try again later."
                : "Sign-in is unavailable right now. Try again in a moment.";
            return View("Index", model);
        }

        [HttpGet("/register")]
        public IActionResult Register(string? returnUrl)
        {
            AccountPage("register", false);
            return View("Index", new AccountPageVm { Mode = "register", Register = new RegisterForm { ReturnUrl = returnUrl } });
        }

        [HttpPost("/register")]
        [EnableRateLimiting(AccountRateLimits.Policy)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register([Bind(Prefix = "Register")] RegisterForm form, CancellationToken cancellationToken)
        {
            AccountPage("register", false);
            var model = new AccountPageVm { Mode = "register", Register = form };
            if (!ModelState.IsValid) return View("Index", model);

            var registered = await _accounts.RegisterAsync(form.Name.Trim(), form.Email.Trim(), form.Password, cancellationToken);
            if (registered.Data is null)
            {
                model.Error = registered.Status == StatusCodes.Status400BadRequest
                    ? registered.FirstError ?? "That account could not be created."
                    : "Registration is unavailable right now. Try again in a moment.";
                return View("Index", model);
            }

            // No sign-in yet: the account waits until the link in the email is opened.
            return RedirectToAction(nameof(CheckEmail), new { email = registered.Data.Email, sent = registered.Data.EmailSent });
        }

        [HttpGet("/register/check-email")]
        public IActionResult CheckEmail(string? email, bool sent = true, bool resent = false)
        {
            ConfirmPage();
            return View("CheckEmail", new CheckEmailVm { Email = email ?? "", EmailSent = sent, Resent = resent });
        }

        // The link in the confirmation email. A link that worked sends the guest to sign in.
        [HttpGet("/confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string? email, string? token, CancellationToken cancellationToken)
        {
            ConfirmPage();
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(token))
                return View("CheckEmail", new CheckEmailVm { Email = email ?? "", LinkError = "That confirmation link is incomplete. Ask for a new one below." });

            var confirmed = await _accounts.ConfirmEmailAsync(email.Trim(), token.Trim(), cancellationToken);
            if (confirmed.Data is not null)
                return RedirectToAction(nameof(SignIn), new { email = confirmed.Data.Email, confirmed = true });

            return View("CheckEmail", new CheckEmailVm
            {
                Email = email.Trim(),
                LinkError = confirmed.Status == StatusCodes.Status400BadRequest
                    ? confirmed.FirstError ?? "That confirmation link is not valid or has expired."
                    : "Confirmation is unavailable right now. Open the link again in a moment."
            });
        }

        // Always answers the same, so the form cannot tell anyone which emails are registered.
        [HttpPost("/confirm-email/resend")]
        [EnableRateLimiting(AccountRateLimits.Policy)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResendConfirmation(string? email, CancellationToken cancellationToken)
        {
            if (!string.IsNullOrWhiteSpace(email)) await _accounts.ResendConfirmationAsync(email.Trim(), cancellationToken);
            return RedirectToAction(nameof(CheckEmail), new { email = email?.Trim(), resent = true });
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

        private void ConfirmPage()
        {
            ViewData["Title"] = "Confirm your email";
            ViewData["Description"] = "Confirm the email for your Portico account.";
            ViewData["Account"] = "register";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#account";
            ViewData["SkipText"] = "Skip to email confirmation";
            ViewData["FooterNote"] = "The confirmation link works once and ends after 24 hours.";
        }

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
