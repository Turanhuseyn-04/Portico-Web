using Microsoft.AspNetCore.Routing;
using Portico.Web.Helpers;
using Portico.Web.Models.Reservations;

namespace Portico.Web.ViewModels.Trips;

// One reservation with the wording the trip pages share.
public class TripVm
{
    public TripVm(TripDto trip, DateOnly today)
    {
        Trip = trip;
        Today = today;
    }

    public TripDto Trip { get; }
    public DateOnly Today { get; }

    public string Where => string.Join(" · ", new[] { Trip.Area, Trip.City, Trip.Country }.Where(part => !string.IsNullOrEmpty(part)));
    public int Nights => Trip.CheckOut.DayNumber - Trip.CheckIn.DayNumber;
    public string Image => StayText.Image(Trip.Image);
    public DateOnly Deadline => Trip.CheckIn.AddDays(-3);
    public bool Released => Trip.Cancelled && Trip.Refund > 0;

    // A cancelled stay with a refund owes nothing.
    public decimal DueTotal => Released ? 0 : Trip.Total;

    public string Formula =>
        $"{StayText.Money(Trip.Nightly)} × {StayText.Count(Nights, "night", "nights")} × {StayText.Count(Trip.Rooms, "room", "rooms")}";

    public string CancellationText => Trip.Cancelled ? "Cancelled" : Trip.Refundable ? StayText.Long(Deadline) : "Non-refundable";
    public bool CancellationLocked => Trip.Cancelled || !Trip.Refundable;

    public string Outcome
    {
        get
        {
            var total = StayText.Money(Trip.Total);
            if (Trip.Cancelled)
                return Released ? $"Cancelled. {total} is released, so the total is now €0." : $"Cancelled. {total} stays due.";
            if (!Trip.Refundable)
                return $"This stay is non-refundable. Cancelling keeps {total} due.";
            return Today <= Deadline
                ? $"Free cancellation until {StayText.Long(Deadline)}. Cancel by then and {total} is released."
                : $"Free cancellation closed on {StayText.Long(Deadline)}. {total} stays due.";
        }
    }

    public RouteValueDictionary PropertyRoute => new()
    {
        ["id"] = Trip.PropertyId,
        ["checkIn"] = StayText.Key(Trip.CheckIn),
        ["checkOut"] = StayText.Key(Trip.CheckOut),
        ["adults"] = Trip.Adults,
        ["rooms"] = Trip.Rooms
    };
}
