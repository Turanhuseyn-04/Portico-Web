using Microsoft.AspNetCore.Routing;
using Portico.Web.Helpers;
using Portico.Web.Models.Stays;

namespace Portico.Web.ViewModels.Property;

// What the guest asked for, as it arrives in the address bar.
public class PropertyQuery
{
    public string? Id { get; set; }
    public string? CheckIn { get; set; }
    public string? CheckOut { get; set; }
    public int? Adults { get; set; }
    public int? Children { get; set; }
    public int? Rooms { get; set; }
    public string? Room { get; set; }
    public bool? Available { get; set; }
    public bool? Fits { get; set; }
    public bool? Refundable { get; set; }
    public bool? Breakfast { get; set; }
    public string? Travellers { get; set; }
    public string? Sort { get; set; }
    public int? Reviews { get; set; }
}

// The same request after it has been checked against the stay: dates that cannot be booked are dropped with a reason.
public class StayChoice
{
    public const int MaxNights = 30;
    public const int ReviewPage = 4;
    public static readonly string[] ReviewSorts = { "recent", "high", "low" };

    public string Id { get; set; } = "";
    public DateOnly? CheckIn { get; set; }
    public DateOnly? CheckOut { get; set; }
    public int Adults { get; set; } = 2;
    public int Children { get; set; }
    public int Rooms { get; set; } = 1;
    public string? Room { get; set; }
    public bool Available { get; set; } = true;
    public bool Fits { get; set; } = true;
    public bool Refundable { get; set; }
    public bool Breakfast { get; set; }
    public string Travellers { get; set; } = "all";
    public string Sort { get; set; } = "recent";
    public int Reviews { get; set; } = ReviewPage;
    public string DateError { get; set; } = "";

    public bool HasDates => CheckIn.HasValue && CheckOut.HasValue;
    public int Nights => HasDates ? CheckOut!.Value.DayNumber - CheckIn!.Value.DayNumber : 0;
    public int People => Adults + Children;

    public static StayChoice From(PropertyQuery query, StayDetail stay, DateOnly today)
    {
        var choice = new StayChoice
        {
            Id = stay.Id,
            Adults = Math.Clamp(query.Adults ?? 2, 1, 8),
            Children = Math.Clamp(query.Children ?? 0, 0, 6),
            Rooms = Math.Clamp(query.Rooms ?? 1, 1, 4),
            Room = stay.Rooms.Any(room => room.Id == query.Room) ? query.Room : null,
            Available = query.Available ?? true,
            Fits = query.Fits ?? true,
            Refundable = query.Refundable ?? false,
            Breakfast = query.Breakfast ?? false,
            Travellers = stay.Reviews.Any(review => review.Traveller == query.Travellers) ? query.Travellers! : "all",
            Sort = ReviewSorts.Contains(query.Sort) ? query.Sort! : "recent",
            Reviews = Math.Max(ReviewPage, query.Reviews ?? ReviewPage)
        };

        if (string.IsNullOrEmpty(query.CheckIn) && string.IsNullOrEmpty(query.CheckOut)) return choice;

        var checkIn = StayText.ReadDay(query.CheckIn);
        var checkOut = StayText.ReadDay(query.CheckOut);
        var minNights = Math.Max(1, stay.Policies.MinNights);
        if (checkIn is null || checkOut is null)
            choice.DateError = "The dates in the link could not be read. Choose new dates.";
        else if (checkIn < today)
            choice.DateError = "The check-in date in the link has passed. Choose new dates.";
        else if (checkOut <= checkIn)
            choice.DateError = "Check-out must be after check-in. Choose new dates.";
        else if (checkOut.Value.DayNumber - checkIn.Value.DayNumber > MaxNights)
            choice.DateError = $"Stays can be up to {MaxNights} nights. Choose new dates.";
        else if (checkOut.Value.DayNumber - checkIn.Value.DayNumber < minNights)
            choice.DateError = $"{stay.Name} has a minimum stay of {minNights} nights. Choose new dates.";
        else
        {
            choice.CheckIn = checkIn;
            choice.CheckOut = checkOut;
        }
        return choice;
    }

    // Route values for a link back to this page, with one change applied. Defaults are left out to keep the address short.
    public RouteValueDictionary Link(Action<StayChoice>? change = null)
    {
        var next = (StayChoice)MemberwiseClone();
        change?.Invoke(next);
        var values = new RouteValueDictionary { ["id"] = next.Id };
        if (next.HasDates)
        {
            values["checkIn"] = StayText.Key(next.CheckIn!.Value);
            values["checkOut"] = StayText.Key(next.CheckOut!.Value);
        }
        values["adults"] = next.Adults;
        if (next.Children > 0) values["children"] = next.Children;
        values["rooms"] = next.Rooms;
        if (next.Room is not null) values["room"] = next.Room;
        if (!next.Available) values["available"] = false;
        if (!next.Fits) values["fits"] = false;
        if (next.Refundable) values["refundable"] = true;
        if (next.Breakfast) values["breakfast"] = true;
        if (next.Travellers != "all") values["travellers"] = next.Travellers;
        if (next.Sort != "recent") values["sort"] = next.Sort;
        if (next.Reviews > ReviewPage) values["reviews"] = next.Reviews;
        return values;
    }
}

public enum RoomState
{
    NoDates,
    Error,
    SoldOut,
    Short,
    Available
}

public class RoomRowVm
{
    public StayRoom Room { get; set; } = null!;
    public RoomState State { get; set; }
    public bool Fits { get; set; }
    public int Left { get; set; }
    public string? SoldOutOn { get; set; }
    public decimal Total { get; set; }
    public bool Selected { get; set; }

    public bool Bookable => State == RoomState.Available && Fits;
    public bool Blocked => State is RoomState.SoldOut or RoomState.Short || !Fits;
}

public record PolicyLine(string Text, bool Locked);

public record ReserveAction(bool Ready, string Problem, bool OpenDates);

public class PropertyPageVm
{
    public StayDetail Stay { get; set; } = null!;
    public StayChoice Choice { get; set; } = null!;
    public TypeCopy Copy { get; set; } = null!;
    public bool OffersFailed { get; set; }
    public List<RoomRowVm> Rooms { get; set; } = new();
    public List<RoomRowVm> ShownRooms { get; set; } = new();
    public List<StayReview> ShownReviews { get; set; } = new();
    public int FilteredReviewCount { get; set; }

    // Whether the signed-in guest saved this stay; null when nobody is signed in.
    public bool? Saved { get; set; }

    public decimal MinPrice => Stay.Rooms.Count == 0 ? 0 : Stay.Rooms.Min(room => room.Price);
    public RoomRowVm? Selected => Rooms.FirstOrDefault(row => row.Selected);
    public string GuestLabel
    {
        get
        {
            var parts = new List<string> { StayText.Count(Choice.Adults, "adult", "adults") };
            if (Choice.Children > 0) parts.Add(StayText.Count(Choice.Children, "child", "children"));
            parts.Add(StayText.Count(Choice.Rooms, Copy.Unit, Copy.Units));
            return string.Join(" · ", parts);
        }
    }

    public string Description =>
        $"{Stay.Summary} {Stay.Type} in {Stay.Address.Area}, {Stay.Address.City}. From {StayText.Money(MinPrice)} a night.";

    public static PropertyPageVm Build(StayDetail stay, StayChoice choice, List<RoomAvailability>? offers, bool offersFailed)
    {
        var model = new PropertyPageVm
        {
            Stay = stay,
            Choice = choice,
            Copy = StayText.Copy(stay.Type),
            OffersFailed = choice.HasDates && offersFailed
        };

        var byRoom = (offers ?? new List<RoomAvailability>()).ToDictionary(offer => offer.RoomId);
        model.Rooms = stay.Rooms.Select(room => model.Row(room, byRoom)).ToList();

        var checkedDates = choice.HasDates && !model.OffersFailed;
        model.ShownRooms = model.Rooms.Where(row =>
                !(choice.Fits && !row.Fits)
                && !(choice.Refundable && !row.Room.Refundable)
                && !(choice.Breakfast && !row.Room.Breakfast)
                && !(choice.Available && checkedDates && !row.Bookable))
            .ToList();

        IEnumerable<StayReview> reviews = stay.Reviews.Where(review => choice.Travellers == "all" || review.Traveller == choice.Travellers);
        reviews = choice.Sort switch
        {
            "high" => reviews.OrderByDescending(review => review.Score),
            "low" => reviews.OrderBy(review => review.Score),
            _ => reviews.OrderByDescending(review => review.Date)
        };
        var filtered = reviews.ToList();
        model.FilteredReviewCount = filtered.Count;
        model.ShownReviews = filtered.Take(choice.Reviews).ToList();
        return model;
    }

    private RoomRowVm Row(StayRoom room, Dictionary<string, RoomAvailability> offers)
    {
        var row = new RoomRowVm
        {
            Room = room,
            Fits = room.Sleeps * Choice.Rooms >= Choice.People,
            Selected = room.Id == Choice.Room
        };
        if (!Choice.HasDates) row.State = RoomState.NoDates;
        else if (OffersFailed) row.State = RoomState.Error;
        else if (!offers.TryGetValue(room.Id, out var offer) || offer.Left == 0)
        {
            row.State = RoomState.SoldOut;
            row.SoldOutOn = offers.TryGetValue(room.Id, out var sold) ? sold.SoldOutOn : null;
        }
        else if (offer.Left < Choice.Rooms)
        {
            row.State = RoomState.Short;
            row.Left = offer.Left;
        }
        else
        {
            row.State = RoomState.Available;
            row.Left = offer.Left;
            row.Total = offer.Nightly * offer.Nights * Choice.Rooms;
        }
        return row;
    }

    public PolicyLine CancelPolicy(StayRoom room, DateOnly today)
    {
        if (!room.Refundable) return new("Non-refundable. You pay the full amount if you cancel.", true);
        if (!Choice.HasDates) return new("Free cancellation until 3 days before check-in.", false);
        var until = Choice.CheckIn!.Value.AddDays(-3);
        if (until < today) return new("Free cancellation has closed for these dates.", true);
        return new($"Free cancellation until {StayText.Pretty(until)}.", false);
    }

    public string SelectionProblem(RoomRowVm? row)
    {
        if (row is null) return "";
        var room = row.Room;
        if (!row.Fits)
            return $"{room.Name} sleeps {room.Sleeps} per {Copy.Unit}. Add a {Copy.Unit} or choose a larger one for {StayText.Count(Choice.People, "guest", "guests")}.";
        if (row.State == RoomState.SoldOut)
            return $"{room.Name} is sold out{SoldOutDay(row.SoldOutOn, " on ", " for these dates")}. Choose another {Copy.Unit} or move your dates.";
        if (row.State == RoomState.Short)
            return $"Only {row.Left} left at this rate for these dates. Lower the number of {Copy.Units} or choose another.";
        return "";
    }

    // What the Reserve button does: go to the reserve page, or say what is missing first.
    public ReserveAction Reserve
    {
        get
        {
            if (!Choice.HasDates) return new(false, "Add your check-in and check-out dates first.", true);
            if (OffersFailed) return new(false, "Availability did not load. Try again in the list below.", false);
            if (Selected is null) return new(false, $"Choose a {Copy.Unit} to reserve.", false);
            var problem = SelectionProblem(Selected);
            return problem == "" ? new(true, "", false) : new(false, problem, false);
        }
    }

    public string BookError => Choice.DateError != "" ? Choice.DateError : SelectionProblem(Selected);

    public decimal ReadyTotal => Selected is { Bookable: true } row ? row.Total : 0;

    public string CancellationRule
    {
        get
        {
            if (Stay.Rooms.All(room => room.Refundable)) return "Free cancellation until 3 days before check-in. After that, the first night is charged.";
            if (Stay.Rooms.Any(room => room.Refundable)) return "Free cancellation until 3 days before check-in on rates marked free cancellation. Non-refundable rates are charged in full if you cancel.";
            return "Every rate here is non-refundable. You pay the full amount if you cancel.";
        }
    }

    public List<(string Term, string Text)> Rules => new()
    {
        ("Check-in", Stay.Policies.CheckIn),
        ("Check-out", Stay.Policies.CheckOut),
        ("Cancellation", CancellationRule),
        ("Minimum stay", StayText.Count(Stay.Policies.MinNights, "night", "nights")),
        ("Children and beds", Stay.Policies.Children),
        ("Age", Stay.Policies.Age),
        ("Pets", Stay.Policies.Pets),
        ("Quiet hours", Stay.Policies.Quiet),
        ("Payment", Stay.Policies.Payment)
    };

    public List<(string Question, string Answer)> Faq
    {
        get
        {
            var policies = Stay.Policies;
            var withBreakfast = Stay.Rooms.Where(room => room.Breakfast).Select(room => room.Name).ToList();
            var far = Stay.Nearby.FirstOrDefault(group => group.Group == "Getting around")?.Items.LastOrDefault();
            var faqs = new List<(string, string)>
            {
                ($"What time is check-in at {Stay.Name}?", $"Check-in is {StayText.LowerFirst(policies.CheckIn)}. Check-out is {StayText.LowerFirst(policies.CheckOut)}."),
                ("Is breakfast included?", withBreakfast.Count > 0 ? $"Breakfast is included with {string.Join(", ", withBreakfast)}." : "Breakfast is not included in any rate."),
                ("Can I cancel for free?", CancellationRule)
            };
            if (far is { Count: >= 2 }) faqs.Add(($"How far is {far[0]}?", $"{far[0]} is {far[1]} from {Stay.Name}."));
            faqs.Add(("Are pets allowed?", policies.Pets));
            faqs.Add(("How is the total calculated?", $"The total is the nightly rate × the number of nights × the number of {Copy.Units}. The total you see before you reserve is the total you pay at the property."));
            return faqs;
        }
    }

    public static string SoldOutDay(string? key, string prefix, string fallback) =>
        StayText.ReadDay(key) is { } day ? prefix + StayText.Pretty(day) : fallback;
}
