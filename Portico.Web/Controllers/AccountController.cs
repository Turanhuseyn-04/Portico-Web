using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Controllers
{
    public class AccountController : Controller
    {
        [HttpGet("/signin")]
        public IActionResult SignIn(string? email)
        {
            return Account("signin", false, email);
        }

        [HttpGet("/signin/failed")]
        public IActionResult SignInFailed(string? email)
        {
            return Account("signin", true, email);
        }

        [HttpGet("/register")]
        public IActionResult Register()
        {
            return Account("register", false, null);
        }

        private IActionResult Account(string mode, bool failed, string? email)
        {
            ViewData["Title"] = failed ? "Sign in failed" : mode == "register" ? "Create an account" : "Sign in";
            ViewData["Description"] = mode == "register"
                ? "Create a Portico account. This preview stores nothing."
                : "Sign in to Portico. A failed login stays on its own address.";
            ViewData["Account"] = mode == "register" ? "register" : "signin";
            ViewData["Mode"] = mode;
            ViewData["Failed"] = failed;
            ViewData["Email"] = email ?? "";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#account";
            ViewData["SkipText"] = mode == "register" ? "Skip to create an account" : "Skip to sign in";
            ViewData["FooterNote"] = mode == "register"
                ? "Create an account is its own address. Nothing is stored until Portico.Api is connected."
                : "Sign in is its own address. A password that does not match opens the failed-login address. Nothing is stored until Portico.Api is connected.";
            return View("Index");
        }
    }
}
