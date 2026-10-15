using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Controllers
{
    public class ReservationController : Controller
    {
        [HttpGet("/reservation/{reference?}")]
        public IActionResult Index(string? reference)
        {
            ViewData["Title"] = string.IsNullOrWhiteSpace(reference) ? "Reservation" : reference;
            ViewData["Description"] = "One Portico reservation: the property, the room, the dates, the cancellation date, and the total.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#reservation";
            ViewData["SkipText"] = "Skip to the reservation";
            ViewData["FooterNote"] = "One reservation, opened by its reference. The record is a preview until Portico.Api supplies it.";
            ViewData["Reference"] = reference ?? "";
            return View();
        }
    }
}
