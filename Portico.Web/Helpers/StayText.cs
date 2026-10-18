using System.Globalization;

namespace Portico.Web.Helpers;

public sealed record TypeCopy(string Noun, string Unit, string Units, string Options, string Choose)
{
    public string UnitsTitle => char.ToUpperInvariant(Units[0]) + Units[1..];
}

// Formatting shared by the guest pages, so prices and dates read the same as the old scripts.
public static class StayText
{
    private static readonly CultureInfo English = CultureInfo.GetCultureInfo("en-GB");
    private static readonly NumberFormatInfo Euro = EuroFormat();

    private static readonly Dictionary<string, TypeCopy> Copies = new()
    {
        ["Hotel"] = new("hotel", "room", "rooms", "rooms", "Choose your room"),
        ["Resort"] = new("resort", "room", "rooms", "rooms", "Choose your room"),
        ["Apartment"] = new("apartment", "apartment", "apartments", "apartments", "Choose your apartment"),
        ["Villa"] = new("villa", "house", "houses", "rates", "Choose your rate")
    };

    public static readonly IReadOnlyDictionary<string, string> TypePlurals = new Dictionary<string, string>
    {
        ["Hotel"] = "Hotels",
        ["Apartment"] = "Apartments",
        ["Resort"] = "Resorts",
        ["Villa"] = "Villas"
    };

    public static TypeCopy Copy(string? type) =>
        type is not null && Copies.TryGetValue(type, out var copy) ? copy : Copies["Hotel"];

    public static string Money(decimal amount) => amount.ToString("C0", Euro);

    public static string Pretty(DateOnly date) => date.ToString("d MMM yyyy", English);

    public static string Short(DateOnly date) => date.ToString("ddd d MMM", English);

    public static string MonthYear(DateOnly date) => date.ToString("MMMM yyyy", English);

    public static string Key(DateOnly date) => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);

    public static DateOnly? ReadDay(string? key) =>
        DateOnly.TryParseExact(key, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var date) ? date : null;

    public static string Score(decimal score) =>
        score == 10 ? "10" : score.ToString("0.0", CultureInfo.InvariantCulture);

    public static string ScoreLabel(decimal score) =>
        score >= 9 ? "Wonderful" : score >= 8 ? "Excellent" : "Good";

    public static string Count(int value, string one, string many) =>
        $"{value} {(value == 1 ? one : many)}";

    public static string LowerFirst(string text) =>
        string.IsNullOrEmpty(text) ? text : char.ToLowerInvariant(text[0]) + text[1..];

    public static string Initials(string name)
    {
        var letters = string.Concat(name.Split(' ', StringSplitOptions.RemoveEmptyEntries).Select(part => part[0])).ToUpperInvariant();
        return letters.Length > 2 ? letters[..2] : letters;
    }

    private static NumberFormatInfo EuroFormat()
    {
        var format = (NumberFormatInfo)English.NumberFormat.Clone();
        format.CurrencySymbol = "€";
        return format;
    }
}
