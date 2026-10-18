using Microsoft.AspNetCore.Mvc;
using Portico.Web.Models;
using Portico.Web.Services;
using Portico.Web.ViewModels.Trips;

namespace Portico.Web.Controllers
{
    public class ReservationController : Controller
    {
        private readonly IReservationApi _reservations;

        public ReservationController(IReservationApi reservations)
        {
            _reservations = reservations;
        }

        private static DateOnly Today => DateOnly.FromDateTime(DateTime.Today);

        [HttpGet("/trips")]
        public async Task<IActionResult> Trips(CancellationToken cancellationToken)
        {
            ViewData["Title"] = "Your trips";
            ViewData["Description"] = "The stays you have reserved, each with its reference, dates, property, and total.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#trips";
            ViewData["SkipText"] = "Skip to your trips";
            ViewData["TripsNav"] = true;
            ViewData["FooterNote"] = "Each stay keeps its reference, dates, property, and total.";

            var trips = await _reservations.ListAsync(cancellationToken);
            if (trips.Data is null)
                return Missing("trips", "Account", "Your trips did not load", "Portico.Api did not answer. Try again in a moment.", Request.Path, "Try again");

            return View(trips.Data.OrderBy(trip => trip.CheckIn).Select(trip => new TripVm(trip, Today)).ToList());
        }

        [HttpGet("/reservation/{reference}/cancel")]
        public async Task<IActionResult> Cancel(string reference, CancellationToken cancellationToken)
        {
            CancelPage(reference);
            var (trip, stop) = await LoadAsync(reference, "cancel", cancellationToken);
            return trip is null ? stop! : View(trip);
        }

        [HttpPost("/reservation/{reference}/cancel")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(string reference, CancellationToken cancellationToken)
        {
            CancelPage(reference);
            var cancelled = await _reservations.CancelAsync(reference, cancellationToken);
            if (cancelled.Data is null)
            {
                return cancelled.IsNotFound
                    ? NotFoundMessage(reference, "cancel")
                    : Missing("cancel", "Reservation", "The cancellation did not go through", "Portico.Api did not answer. The reservation is unchanged. Try again.", Url.Action(nameof(Cancel), new { reference })!, "Back to the cancellation");
            }

            return RedirectToAction(nameof(Cancel), new { reference });
        }

        [HttpGet("/reservation/{reference?}")]
        public async Task<IActionResult> Index(string? reference, CancellationToken cancellationToken)
        {
            ViewData["Title"] = string.IsNullOrWhiteSpace(reference) ? "Reservation" : reference;
            ViewData["Description"] = "One Portico reservation: the property, the room, the dates, the cancellation date, and the total.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#reservation";
            ViewData["SkipText"] = "Skip to the reservation";
            ViewData["FooterNote"] = "One reservation, opened by its reference.";

            var (trip, stop) = await LoadAsync(reference, "reservation", cancellationToken);
            return trip is null ? stop! : View(trip);
        }

        private async Task<(TripVm? Trip, IActionResult? Stop)> LoadAsync(string? reference, string section, CancellationToken cancellationToken)
        {
            if (string.IsNullOrWhiteSpace(reference))
                return (null, Missing(section, "Reservation", "Open a reference", "A confirmation email opens one reservation by its reference.", Url.Action(nameof(Trips))!, "Your trips"));

            var trip = await _reservations.GetAsync(reference.Trim(), cancellationToken);
            if (trip.Data is not null) return (new TripVm(trip.Data, Today), null);

            return trip.IsNotFound
                ? (null, NotFoundMessage(reference, section))
                : (null, Missing(section, "Reservation", "This reservation did not load", "Portico.Api did not answer. Try again in a moment.", Request.Path, "Try again"));
        }

        private IActionResult NotFoundMessage(string reference, string section)
        {
            Response.StatusCode = StatusCodes.Status404NotFound;
            return Missing(section, "Reservation", "No reservation for this reference", $"{reference} is not a Portico reservation. Check the reference in your confirmation.", Url.Action(nameof(Trips))!, "Your trips");
        }

        private ViewResult Missing(string section, string kicker, string title, string text, string href, string linkText)
        {
            ViewData["Title"] = title;
            return View("Message", new PageMessage(section, kicker, title, text, href, linkText));
        }

        private void CancelPage(string reference)
        {
            ViewData["Title"] = $"Cancel {reference}";
            ViewData["Description"] = "Cancel one Portico reservation. The page shows the stay, the deadline, and what happens to the total.";
            ViewData["BodyClass"] = "results-page";
            ViewData["SolidHeader"] = true;
            ViewData["SkipHref"] = "#cancel";
            ViewData["SkipText"] = "Skip to cancellation";
            ViewData["FooterNote"] = "Cancel one reservation. The deadline says whether the total is released.";
        }
    }
}
