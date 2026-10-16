using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Areas.Admin.Controllers
{
    public class DashboardController : AdminControllerBase
    {
        public IActionResult Index()
        {
            Page(
                "Admin",
                "Reservations in this browser, the eight stays, and the account kept here.",
                "#admin",
                "Skip to admin",
                "Reservations, the eight stays, and the account in this browser. Prices and rooms stay read-only until Portico.Api can save an edit.");
            return View();
        }
    }
}
