using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Areas.Admin.Controllers
{
    public class ReportController : AdminControllerBase
    {
        public IActionResult Index()
        {
            Page(
                "Report",
                "Nights sold, money still due, and cancellations for one month.",
                "#adminReport",
                "Skip to the report",
                "Report");
            return View();
        }
    }
}
