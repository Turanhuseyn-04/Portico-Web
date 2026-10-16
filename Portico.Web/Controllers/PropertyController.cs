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

        [HttpGet("/reserve")]
        public IActionResult Reserve()
        {
            ViewData["Title"] = "Confirm your stay";
            ViewData["Description"] = "Confirm one stay: the property, room, dates, and the name the confirmation is sent to.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#reserve";
            ViewData["SkipText"] = "Skip to confirmation";
            ViewData["FooterNote"] = "Confirm the property, room, and dates, then the name the confirmation is sent to. Payment is the next page. Nothing is charged until Portico.Api takes it.";
            return View();
        }

        [HttpGet("/payment")]
        public IActionResult Payment()
        {
            ViewData["Title"] = "Payment";
            ViewData["Description"] = "Pay for one stay. The page shows the same total as the reservation and a card form. Nothing is charged until Portico.Api takes payment.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#payment";
            ViewData["SkipText"] = "Skip to payment";
            ViewData["FooterNote"] = "The card form sits after the guest details. The total matches the stay. Nothing is charged until Portico.Api takes payment.";
            return View();
        }
    }
}
