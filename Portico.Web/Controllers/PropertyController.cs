using Microsoft.AspNetCore.Mvc;
using Portico.Web.Services;
using Portico.Web.ViewModels.Property;

namespace Portico.Web.Controllers
{
    public class PropertyController : Controller
    {
        private readonly IPropertyApi _properties;

        public PropertyController(IPropertyApi properties)
        {
            _properties = properties;
        }

        [HttpGet("/property")]
        public async Task<IActionResult> Index([FromQuery] PropertyQuery query, CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Stay details";
            ViewData["Description"] = "Photos, rooms, prices, reviews and location for a stay on Portico. The total is nights times the nightly rate.";
            ViewData["BodyClass"] = "property-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#rooms";
            ViewData["SkipText"] = "Skip to rooms";
            ViewData["FooterNote"] = "Rooms, prices and availability come from Portico.Api. Nothing is charged.";

            if (string.IsNullOrWhiteSpace(query.Id))
                return RedirectToAction("Index", "Home", null, "list");

            var stay = await _properties.GetAsync(query.Id, cancellationToken);
            if (stay.Data is null)
            {
                ViewData["Title"] = stay.IsNotFound ? "Stay not found" : "Stay did not load";
                Response.StatusCode = stay.IsNotFound ? StatusCodes.Status404NotFound : StatusCodes.Status503ServiceUnavailable;
                return View("Unavailable", stay.IsNotFound);
            }

            var choice = StayChoice.From(query, stay.Data, DateOnly.FromDateTime(DateTime.Today));
            var offersFailed = false;
            List<Models.Stays.RoomAvailability>? offers = null;
            if (choice.HasDates)
            {
                var availability = await _properties.AvailabilityAsync(stay.Data.Id, choice.CheckIn!.Value, choice.CheckOut!.Value, cancellationToken);
                offers = availability.Data;
                offersFailed = offers is null;
            }

            var model = PropertyPageVm.Build(stay.Data, choice, offers, offersFailed);
            ViewData["Title"] = $"{stay.Data.Name}, {stay.Data.Address.City}";
            ViewData["Description"] = model.Description;
            return View(model);
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
