using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Areas.Admin.Controllers
{
    public class StayController : AdminControllerBase
    {
        public IActionResult Index(string id)
        {
            ViewData["StayId"] = id;
            Page(
                "Stay",
                "One stay in the catalog: address, house rules for arrival, and every room.",
                "#adminStay",
                "Skip to the stay",
                "Address, arrival times, and every room. The public page is linked from here.");
            return View();
        }
    }
}
