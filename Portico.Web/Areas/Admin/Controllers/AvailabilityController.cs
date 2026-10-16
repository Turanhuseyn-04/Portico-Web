using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Areas.Admin.Controllers
{
    public class AvailabilityController : AdminControllerBase
    {
        public IActionResult Index(string id)
        {
            ViewData["StayId"] = id;
            Page(
                "Availability",
                "Which rooms are free on which nights, and where reservations overlap the rooms for sale.",
                "#adminAvailability",
                "Skip to availability",
                "Availability");
            return View();
        }
    }
}
