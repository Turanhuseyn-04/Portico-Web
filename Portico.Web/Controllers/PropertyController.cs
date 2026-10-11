using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Controllers
{
    public class PropertyController : Controller
    {
        [HttpGet("/property")]
        public IActionResult Index(string? id)
        {
            ViewData["Title"] = "Stay details";
            ViewData["Description"] = "Photos, rooms, prices, reviews and location for a stay on Portico. The total is nights times the nightly rate.";
            ViewData["BodyClass"] = "property-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#rooms";
            ViewData["SkipText"] = "Skip to rooms";
            ViewData["FooterNote"] = "Rooms, prices and availability come from mock data until the API is in place. Nothing is charged.";
            ViewData["StayId"] = id;
            return View();
        }
    }
}
