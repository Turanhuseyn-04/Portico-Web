namespace Portico.Web.Models.Stays;

// The shapes Portico.Api returns from /properties. Names follow the API so JSON binds without mapping.

public sealed record StayListItem(
    string Id,
    string Name,
    string Type,
    int? Stars,
    StayAddress Address,
    StayLocation Location,
    decimal Score,
    int ReviewCount,
    string Summary,
    List<StayPhoto> Gallery,
    List<StayRoom> Rooms);

public sealed record StayDetail(
    string Id,
    string Name,
    string Type,
    int? Stars,
    StayAddress Address,
    StayLocation Location,
    decimal Score,
    int ReviewCount,
    Dictionary<string, decimal> Scores,
    string Summary,
    List<string> Description,
    List<StayHighlight> Highlights,
    List<string> Popular,
    List<StayAmenityGroup> AmenityGroups,
    List<StayPhoto> Gallery,
    List<StayRoom> Rooms,
    List<StayNearby> Nearby,
    StayPolicies Policies,
    List<StayReview> Reviews);

public sealed record StayAddress(string Street, string Area, string City, string Postcode, string Country);

public sealed record StayLocation(double Lat, double Lng);

public sealed record StayPhoto(string Src, string Alt);

public sealed record StayRoom(
    string Id,
    string Name,
    string Image,
    int Size,
    string Beds,
    int Sleeps,
    decimal Price,
    bool Refundable,
    bool Breakfast,
    int Inventory,
    List<string> Features);

public sealed record StayHighlight(string Icon, string Title, string Text);

public sealed record StayAmenityGroup(string Name, List<string> Items);

public sealed record StayNearby(string Group, List<List<string>> Items);

public sealed record StayPolicies(
    string CheckIn,
    string CheckOut,
    string Children,
    string Age,
    string Pets,
    string Quiet,
    string Payment,
    int MinNights);

public sealed record StayReview(
    string Author,
    string Country,
    string Traveller,
    string? RoomId,
    int Nights,
    DateOnly Date,
    decimal Score,
    string Title,
    string Liked,
    string Disliked);

public sealed record RoomAvailability(
    string RoomId,
    int Left,
    string? SoldOutOn,
    int Nights,
    decimal Nightly,
    decimal Total,
    Dictionary<string, int> PerNight);
