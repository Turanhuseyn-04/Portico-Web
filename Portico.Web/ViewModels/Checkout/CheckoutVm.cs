using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Routing;
using Portico.Web.Helpers;
using Portico.Web.Models.Reservations;
using Portico.Web.Models.Stays;

namespace Portico.Web.ViewModels.Checkout;

// The stay being booked, as the reserve and payment addresses carry it.
public class StayPickQuery
{
    public string? Id { get; set; }
    public string? Room { get; set; }
    public string? CheckIn { get; set; }
    public string? CheckOut { get; set; }
    public int? Adults { get; set; }
    public int? Children { get; set; }
    public int? Rooms { get; set; }

    public StayPick? Read()
    {
        var checkIn = StayText.ReadDay(CheckIn);
        var checkOut = StayText.ReadDay(CheckOut);
        if (string.IsNullOrWhiteSpace(Id) || string.IsNullOrWhiteSpace(Room) || checkIn is null || checkOut is null || checkOut <= checkIn)
            return null;
        return new StayPick(
            Id,
            Room,
            checkIn.Value,
            checkOut.Value,
            Math.Clamp(Adults ?? 2, 1, 8),
            Math.Clamp(Children ?? 0, 0, 6),
            Math.Clamp(Rooms ?? 1, 1, 4));
    }
}

public sealed record StayPick(string Id, string RoomId, DateOnly CheckIn, DateOnly CheckOut, int Adults, int Children, int Rooms)
{
    public int Nights => CheckOut.DayNumber - CheckIn.DayNumber;

    public RouteValueDictionary Route()
    {
        var values = new RouteValueDictionary
        {
            ["id"] = Id,
            ["room"] = RoomId,
            ["checkIn"] = StayText.Key(CheckIn),
            ["checkOut"] = StayText.Key(CheckOut),
            ["adults"] = Adults,
            ["rooms"] = Rooms
        };
        if (Children > 0) values["children"] = Children;
        return values;
    }

    public StayRequest ToRequest(GuestForm guest) =>
        new(Id, RoomId, CheckIn, CheckOut, Rooms, Adults, Children, guest.Name.Trim(), guest.Email.Trim());
}

public class GuestForm
{
    [Required(ErrorMessage = "Enter the name of the main guest.")]
    [MinLength(2, ErrorMessage = "Enter the name of the main guest.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Enter a valid email address for the confirmation.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address for the confirmation.")]
    public string Email { get; set; } = "";

    public string? Phone { get; set; }

    [StringLength(400, ErrorMessage = "Keep requests under 400 characters.")]
    public string? Requests { get; set; }
}

// The card is checked here and never stored or sent on. Portico.Api does not take payment yet.
public class CardForm
{
    public string CardName { get; set; } = "";
    public string Number { get; set; } = "";
    public string Expiry { get; set; } = "";
    public string Cvc { get; set; } = "";

    public Dictionary<string, string> Problems(DateOnly today)
    {
        var problems = new Dictionary<string, string>();
        var number = Digits(Number);
        var expiry = Digits(Expiry);
        var cvc = Digits(Cvc);

        if ((CardName ?? "").Trim().Length < 2) problems[nameof(CardName)] = "Enter the name on the card.";
        if (number.Length is < 13 or > 19 || !Luhn(number)) problems[nameof(Number)] = "Enter a card number.";
        if (!ExpiryOpen(expiry, today)) problems[nameof(Expiry)] = "Enter an expiry date that has not passed.";
        if (cvc.Length is not (3 or 4)) problems[nameof(Cvc)] = "Enter the security code.";
        return problems;
    }

    private static string Digits(string? value) => new((value ?? "").Where(char.IsAsciiDigit).ToArray());

    private static bool Luhn(string number)
    {
        var sum = 0;
        var alt = false;
        for (var i = number.Length - 1; i >= 0; i--)
        {
            var n = number[i] - '0';
            if (alt)
            {
                n *= 2;
                if (n > 9) n -= 9;
            }
            sum += n;
            alt = !alt;
        }
        return sum % 10 == 0;
    }

    private static bool ExpiryOpen(string digits, DateOnly today)
    {
        if (digits.Length < 4) return false;
        var month = int.Parse(digits[..2]);
        var year = 2000 + int.Parse(digits[2..4]);
        if (month is < 1 or > 12) return false;
        return year * 12 + (month - 1) >= today.Year * 12 + (today.Month - 1);
    }
}

public class CheckoutVm
{
    public StayDetail Stay { get; set; } = null!;
    public StayRoom Room { get; set; } = null!;
    public StayPick Pick { get; set; } = null!;
    public GuestForm Guest { get; set; } = new();
    public CardForm Card { get; set; } = new();
    public string? Error { get; set; }

    public TypeCopy Copy => StayText.Copy(Stay.Type);
    public decimal Total => Room.Price * Pick.Nights * Pick.Rooms;
    public string Where => string.Join(" · ", new[] { Stay.Address.Area, Stay.Address.City, Stay.Address.Country }.Where(part => !string.IsNullOrEmpty(part)));
    public string RoomsLabel => StayText.Count(Pick.Rooms, Copy.Unit, Copy.Units);
    public string Formula => $"{StayText.Money(Room.Price)} × {StayText.Count(Pick.Nights, "night", "nights")} × {RoomsLabel}";

    public string GuestLabel
    {
        get
        {
            var parts = new List<string> { StayText.Count(Pick.Adults, "adult", "adults") };
            if (Pick.Children > 0) parts.Add(StayText.Count(Pick.Children, "child", "children"));
            parts.Add(RoomsLabel);
            return string.Join(" · ", parts);
        }
    }

    public string CancelText => Room.Refundable
        ? $"Free cancellation until {StayText.Long(Pick.CheckIn.AddDays(-3))}."
        : "Non-refundable. You pay the full amount if you cancel.";
}
