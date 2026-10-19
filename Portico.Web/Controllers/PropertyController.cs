using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Web.Models;
using Portico.Web.Models.Accounts;
using Portico.Web.Services;
using Portico.Web.ViewModels.Checkout;
using Portico.Web.ViewModels.Property;

namespace Portico.Web.Controllers
{
    public class PropertyController : Controller
    {
        private readonly IPropertyApi _properties;
        private readonly IReservationApi _reservations;
        private readonly IAccountApi _accounts;

        public PropertyController(IPropertyApi properties, IReservationApi reservations, IAccountApi accounts)
        {
            _properties = properties;
            _reservations = reservations;
            _accounts = accounts;
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
            if (User.Email() is { } email)
            {
                var saved = await _accounts.SavedAsync(email, cancellationToken);
                model.Saved = saved.Data?.Contains(stay.Data.Id) ?? false;
            }
            ViewData["Title"] = $"{stay.Data.Name}, {stay.Data.Address.City}";
            ViewData["Description"] = model.Description;
            return View(model);
        }

        // Booking needs an account: the reservation is kept under its email, and only that account can open or cancel it.
        [Authorize]
        [HttpGet("/reserve")]
        public async Task<IActionResult> Reserve([FromQuery] StayPickQuery query, CancellationToken cancellationToken)
        {
            ReservePage();
            var (model, stop) = await LoadCheckoutAsync(query, "reserve", "Reserve", cancellationToken);
            if (model is null) return stop!;

            model.Guest = ReadCheckout(model.Pick) ?? new GuestForm { Name = User.DisplayName() };
            model.Guest.Email = User.Email()!;
            return View(model);
        }

        [Authorize]
        [HttpPost("/reserve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reserve([FromForm] StayPickQuery query, GuestForm guest, CancellationToken cancellationToken)
        {
            ReservePage();
            var (model, stop) = await LoadCheckoutAsync(query, "reserve", "Reserve", cancellationToken);
            if (model is null) return stop!;

            ModelState.Remove($"{nameof(CheckoutVm.Guest)}.{nameof(GuestForm.Email)}");
            guest.Email = User.Email()!;
            model.Guest = guest;
            if (!ModelState.IsValid) return View(model);

            var offers = await _properties.AvailabilityAsync(model.Pick.Id, model.Pick.CheckIn, model.Pick.CheckOut, cancellationToken);
            if (offers.Data is null)
            {
                model.Error = "The stay could not be continued. Nothing was charged.";
                return View(model);
            }

            var offer = offers.Data.FirstOrDefault(item => item.RoomId == model.Pick.RoomId);
            if (offer is null || offer.Left < model.Pick.Rooms)
            {
                model.Error = "That room was just taken for these dates. Choose another room or change your dates.";
                return View(model);
            }

            SaveCheckout(model.Pick, guest);
            return RedirectToAction(nameof(Payment), model.Pick.Route());
        }

        [Authorize]
        [HttpGet("/payment")]
        public async Task<IActionResult> Payment([FromQuery] StayPickQuery query, CancellationToken cancellationToken)
        {
            PaymentPage();
            var (model, stop) = await LoadCheckoutAsync(query, "payment", "Payment", cancellationToken);
            if (model is null) return stop!;

            var guest = ReadCheckout(model.Pick);
            if (guest is null) return GuestMissing(model.Pick);

            model.Guest = guest;
            return View(model);
        }

        [Authorize]
        [HttpPost("/payment")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Payment([FromForm] StayPickQuery query, CardForm card, CancellationToken cancellationToken)
        {
            PaymentPage();
            var (model, stop) = await LoadCheckoutAsync(query, "payment", "Payment", cancellationToken);
            if (model is null) return stop!;

            var guest = ReadCheckout(model.Pick);
            if (guest is null) return GuestMissing(model.Pick);

            model.Guest = guest;
            model.Card = card;
            foreach (var (field, problem) in card.Problems(DateOnly.FromDateTime(DateTime.Today)))
                ModelState.AddModelError($"{nameof(CheckoutVm.Card)}.{field}", problem);
            if (!ModelState.IsValid) return View(model);

            var created = await _reservations.CreateAsync(model.Pick.ToRequest(guest), cancellationToken);
            if (created.Data is null)
            {
                model.Error = created.IsConflict || created.IsNotFound
                    ? created.FirstError ?? "That room was just taken for these dates."
                    : "The payment did not go through. Nothing was charged.";
                return View(model);
            }

            TempData.Remove(CheckoutKey);
            return RedirectToAction("Index", "Reservation", new { reference = created.Data.Reference });
        }

        private const string CheckoutKey = "Checkout";

        private sealed record SavedCheckout(StayPick Pick, GuestForm Guest);

        // The guest details wait in TempData between the reserve and payment pages, for this stay only.
        private void SaveCheckout(StayPick pick, GuestForm guest) =>
            TempData[CheckoutKey] = JsonSerializer.Serialize(new SavedCheckout(pick, guest));

        private GuestForm? ReadCheckout(StayPick pick)
        {
            if (TempData.Peek(CheckoutKey) is not string json) return null;
            try
            {
                var saved = JsonSerializer.Deserialize<SavedCheckout>(json);
                return saved is not null && saved.Pick == pick ? saved.Guest : null;
            }
            catch (JsonException)
            {
                return null;
            }
        }

        private async Task<(CheckoutVm? Model, IActionResult? Stop)> LoadCheckoutAsync(StayPickQuery query, string section, string kicker, CancellationToken cancellationToken)
        {
            var home = Url.Action("Index", "Home")!;
            var pick = query.Read();
            if (pick is null)
                return (null, Message(section, kicker, "Choose a stay first", "A reservation starts from a property, a room, and your dates.", home, "Back to search"));

            var stay = await _properties.GetAsync(pick.Id, cancellationToken);
            if (stay.Data is null)
            {
                return stay.IsNotFound
                    ? (null, Message(section, kicker, "That stay is not on the list", "Open the property again and choose a room.", home, "Back to search"))
                    : (null, Message(section, kicker, "This stay did not load", "Portico.Api did not answer. Try again in a moment. Nothing was charged.", Request.Path + Request.QueryString, "Try again"));
            }

            var room = stay.Data.Rooms.FirstOrDefault(item => item.Id == pick.RoomId);
            if (room is null)
                return (null, Message(section, kicker, "That room is not on the list", "Open the property again and choose a room.", Url.Action(nameof(Index), pick.Route())!, "Back to the property"));

            return (new CheckoutVm { Stay = stay.Data, Room = room, Pick = pick }, null);
        }

        private IActionResult GuestMissing(StayPick pick) =>
            Message("payment", "Payment", "Add the guest details first", "The card form follows the name and email for this stay.", Url.Action(nameof(Reserve), pick.Route())!, "Back to guest details");

        private ViewResult Message(string section, string kicker, string title, string text, string href, string linkText)
        {
            ViewData["Title"] = title;
            return View("Message", new PageMessage(section, kicker, title, text, href, linkText));
        }

        private void ReservePage()
        {
            ViewData["Title"] = "Confirm your stay";
            ViewData["Description"] = "Confirm one stay: the property, room, dates, and the name the confirmation is sent to.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#reserve";
            ViewData["SkipText"] = "Skip to confirmation";
            ViewData["FooterNote"] = "Confirm the property, room, and dates, then the name the confirmation is sent to. Payment is the next page.";
        }

        private void PaymentPage()
        {
            ViewData["Title"] = "Payment";
            ViewData["Description"] = "Pay for one stay. The page shows the same total as the reservation and a card form.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#payment";
            ViewData["SkipText"] = "Skip to payment";
            ViewData["FooterNote"] = "The card is checked and not stored. Portico.Api records the reservation; it does not take payment yet, so nothing is charged.";
        }
    }
}
