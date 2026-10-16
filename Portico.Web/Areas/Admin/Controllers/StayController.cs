using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Areas.Admin.Controllers
{
    public class StayController : AdminControllerBase
    {
        public IActionResult Index(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Page(
                    "Stays",
                    "The eight houses: type, city, guest rating, rooms, and the lowest nightly rate.",
                    "#adminHouses",
                    "Skip to stays",
                    "Stays");
                return View("List");
            }

            ViewData["StayId"] = id;
            Page(
                "Stay",
                "One stay: address, arrival times, and every room.",
                "#adminStay",
                "Skip to the stay",
                "Stays");
            return View();
        }
    }
}
