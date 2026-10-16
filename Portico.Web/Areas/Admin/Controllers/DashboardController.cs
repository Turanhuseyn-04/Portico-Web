using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Areas.Admin.Controllers
{
    public class DashboardController : AdminControllerBase
    {
        public IActionResult Index()
        {
            Page(
                "Dashboard",
                "Diagrams for reservations in this browser, the stay catalog, and the money still due.",
                "#adminDesk",
                "Skip to the dashboard",
                "Dashboard");
            return View();
        }
    }
}
