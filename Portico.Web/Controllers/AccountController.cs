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

        [HttpGet("/account")]
        public IActionResult Profile()
        {
            ViewData["Title"] = "Your account";
            ViewData["Description"] = "The name and email for this browser, with links to your trips and saved stays.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#account";
            ViewData["SkipText"] = "Skip to your account";
            ViewData["Account"] = "profile";
            ViewData["FooterNote"] = "The name and email stay in this browser until Portico.Api keeps the account. The password is not stored.";
            return View("Profile");
        }

        [HttpGet("/saved")]
        public IActionResult Saved()
        {
            ViewData["Title"] = "Saved stays";
            ViewData["Description"] = "Stays saved from a property page in this browser.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#saved";
            ViewData["SkipText"] = "Skip to saved stays";
            ViewData["FooterNote"] = "A heart on a property page keeps that stay here until Portico.Api stores it.";
            return View("Saved");
        }

        private IActionResult Account(string mode, bool failed, string? email)
        {
            ViewData["Title"] = failed ? "Sign in failed" : mode == "register" ? "Create an account" : "Sign in";
            ViewData["Description"] = mode == "register"
                ? "Create a Portico account. The name and email stay in this browser."
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
                ? "Create an account keeps the name and email in this browser and opens the account page. The password is not stored."
                : "A registered email opens the account page. Any other email opens the failed-login address. The password is not stored.";
            return View("Index");
        }
    }
}
