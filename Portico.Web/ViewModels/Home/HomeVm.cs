using Portico.Web.Helpers;
using Portico.Web.Models.Stays;

namespace Portico.Web.ViewModels.Home;

public class StayCardVm
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public string Area { get; set; } = "";
    public string Kind { get; set; } = "";
    public string Property { get; set; } = "";
    public decimal Score { get; set; }
    public int Reviews { get; set; }
    public decimal Price { get; set; }
    public int MaxGuests { get; set; }
    public bool Cancel { get; set; }
    public bool Scarce { get; set; }
    public List<string> Amenities { get; set; } = new();
    public string Blurb { get; set; } = "";
    public string Image { get; set; } = "";
    public int Order { get; set; }

    public string ScoreText => Score.ToString("0.0", System.Globalization.CultureInfo.InvariantCulture);
    public string CancelText => Cancel ? "Free cancellation until 3 days before check-in." : "Non-refundable once booked.";

    // Words the list cards show beside each stay. Prices, scores, and cities come from the API.
    private static readonly Dictionary<string, (string Kind, bool Scarce, string[] Amenities, string Blurb)> Faces = new()
    {
        ["marble"] = ("Suite", true, new[] { "Courtyard", "Breakfast", "River walk" }, "A stone suite behind a working portico, one street up from the river."),
        ["screens"] = ("Room", false, new[] { "Cedar bath", "Garden", "Work desk" }, "A small room with paper screens and a cedar bath down the hall."),
        ["atelier"] = ("Studio", false, new[] { "Work desk", "Courtyard", "Bike" }, "A studio with a work table, a good lamp, and a quiet courtyard."),
        ["riad"] = ("House", false, new[] { "Whole house", "Roof terrace", "Breakfast" }, "A whole riad: four small doors, one orange tree, a roof for breakfast."),
        ["orchard"] = ("Suite", true, new[] { "Sea view", "Breakfast", "Terrace" }, "A lemon-scented suite on the cliff path, with the port below."),
        ["patio"] = ("Room", false, new[] { "Patio", "Fan", "Walk to market" }, "A tiled room on a patio that stays cool after lunch."),
        ["greenhouse"] = ("Studio", false, new[] { "Kitchenette", "Courtyard", "Work desk" }, "A glass-roofed studio two blocks from the bakery you will use."),
        ["gallery"] = ("House", false, new[] { "Whole house", "Fireplace", "Old Town" }, "A long stone gallery of a house, with a fire and a steep stair.")
    };

    public static StayCardVm From(StayListItem stay, int order)
    {
        var hasFace = Faces.TryGetValue(stay.Id, out var face);
        return new StayCardVm
        {
            Id = stay.Id,
            Name = stay.Name,
            City = stay.Address.City,
            Country = stay.Address.Country,
            Area = stay.Address.Area,
            Kind = hasFace ? face.Kind : "Room",
            Property = stay.Type,
            Score = stay.Score,
            Reviews = stay.ReviewCount,
            Price = stay.Rooms.Count == 0 ? 0 : stay.Rooms.Min(room => room.Price),
            MaxGuests = stay.Rooms.Count == 0 ? 0 : stay.Rooms.Max(room => room.Sleeps),
            Cancel = stay.Rooms.Any(room => room.Refundable),
            Scarce = hasFace && face.Scarce,
            Amenities = hasFace ? face.Amenities.ToList() : new List<string>(),
            Blurb = hasFace ? face.Blurb : stay.Summary,
            Image = stay.Gallery.FirstOrDefault()?.Src ?? "",
            Order = order
        };
    }
}

public record WhereOptionVm(string City, string Country, int Count);

public record DestinationVm(string City, string Image);

public class HeroSearchVm
{
    public string Property { get; set; } = "";
    public List<WhereOptionVm> Places { get; set; } = new();
}

public class StayListVm
{
    public List<StayCardVm> Items { get; set; } = new();
    public string Sort { get; set; } = "featured";
    public string Property { get; set; } = "";
    public string Meta { get; set; } = "";
    public string? LoadError { get; set; }
}

public class HomeVm
{
    public static readonly string[] Sorts = { "featured", "price", "rating" };

    public HeroSearchVm Search { get; set; } = new();
    public StayListVm List { get; set; } = new();

    public List<DestinationVm> Destinations { get; set; } = new()
    {
        new("Rome", "/assets/rome.jpg"),
        new("Athens", "/assets/athens.jpg"),
        new("Paris", "/assets/paris.jpg"),
        new("Istanbul", "/assets/istanbul.jpg"),
        new("Venice", "/assets/venice.jpg"),
        new("Lisbon", "/assets/lisbon-city.jpg"),
        new("Kyoto", "/assets/kyoto-city.jpg"),
        new("Marrakech", "/assets/marrakech-city.jpg")
    };

    public static HomeVm Build(List<StayListItem>? stays, string? sort, string? property)
    {
        var chosenSort = Sorts.Contains(sort) ? sort! : "featured";
        var chosenType = property is not null && StayText.TypePlurals.ContainsKey(property) ? property : "";
        var model = new HomeVm
        {
            Search = new HeroSearchVm { Property = chosenType },
            List = new StayListVm { Sort = chosenSort, Property = chosenType }
        };

        if (stays is null)
        {
            model.List.LoadError = "The stay list did not load. Check that Portico.Api is running, then try again.";
            return model;
        }

        var cards = stays.Select(StayCardVm.From).ToList();
        model.Search.Places = cards
            .GroupBy(card => card.City)
            .Select(group => new WhereOptionVm(group.Key, group.First().Country, group.Count()))
            .Prepend(new WhereOptionVm("Anywhere", "The full short list", cards.Count))
            .ToList();

        IEnumerable<StayCardVm> shown = cards.Where(card => chosenType == "" || card.Property == chosenType);
        shown = chosenSort switch
        {
            "price" => shown.OrderBy(card => card.Price),
            "rating" => shown.OrderByDescending(card => card.Score),
            _ => shown.OrderBy(card => card.Order)
        };
        model.List.Items = shown.ToList();
        model.List.Meta = chosenType == ""
            ? $"{cards.Count} houses. Add dates to turn the nightly rate into a stay total."
            : $"{model.List.Items.Count} of {cards.Count} houses · {StayText.TypePlurals[chosenType]}. Add dates to price the stay.";
        return model;
    }
}
