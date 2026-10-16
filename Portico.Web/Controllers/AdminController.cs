using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Controllers
{
    public class AdminController : Controller
    {
        [HttpGet("/admin")]
        public IActionResult Index()
        {
            ViewData["Title"] = "Admin";
            ViewData["Description"] = "Reservations in this browser, the eight stays, and the account kept here.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#admin";
            ViewData["SkipText"] = "Skip to admin";
            ViewData["AdminNav"] = true;
            ViewData["FooterNote"] = "Reservations, the eight stays, and the account in this browser. Prices and rooms stay read-only until Portico.Api can save an edit.";
            return View();
        }

        [HttpGet("/admin/reservation/{reference}")]
        public IActionResult Reservation(string reference)
        {
            ViewData["Title"] = "Reservation";
            ViewData["Description"] = "One reservation for the person who runs the houses: guest, stay, dates, cancellation, and total.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#adminReservation";
            ViewData["SkipText"] = "Skip to the reservation";
            ViewData["AdminNav"] = true;
            ViewData["Reference"] = reference;
            ViewData["FooterNote"] = "One reservation, with the guest, the stay, the dates, and what happens to the total.";
            return View();
        }

        [HttpGet("/admin/stay/{id}")]
        public IActionResult Stay(string id)
        {
            ViewData["Title"] = "Stay";
            ViewData["Description"] = "One stay in the catalog: address, house rules for arrival, and every room.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#adminStay";
            ViewData["SkipText"] = "Skip to the stay";
            ViewData["AdminNav"] = true;
            ViewData["StayId"] = id;
            ViewData["FooterNote"] = "Address, arrival times, and every room. The public page is linked from here.";
            return View();
        }
    }
}
