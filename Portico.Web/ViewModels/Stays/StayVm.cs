using System.ComponentModel.DataAnnotations;
using Portico.Web.Helpers;
using Portico.Web.ViewModels.Photos;

namespace Portico.Web.ViewModels.Stays;

public class RoomFormVm
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public decimal Price { get; set; }
    public int Sleeps { get; set; }
    public bool Refundable { get; set; }
    public bool Breakfast { get; set; }
    public int Inventory { get; set; }
}

public class StayVm
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int? Stars { get; set; }
    public string Street { get; set; } = "";
    public string Area { get; set; } = "";
    public string City { get; set; } = "";
    public string Postcode { get; set; } = "";
    public string Country { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Summary { get; set; } = "";
    public List<string> Description { get; set; } = new();
    public List<RoomFormVm> Rooms { get; set; } = new();
}

public class StayWriteVm
{
    public string Id { get; set; } = "";
    public string Name { get; set; } = "";
    public string Type { get; set; } = "";
    public int? Stars { get; set; }
    public string Street { get; set; } = "";
    public string Area { get; set; } = "";
    public string City { get; set; } = "";
    public string Postcode { get; set; } = "";
    public string Country { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Summary { get; set; } = "";
    public List<string> Description { get; set; } = new();
    public List<RoomFormVm> Rooms { get; set; } = new();
}

public class StayFormVm
{
    public bool LockId { get; set; }

    [Required(ErrorMessage = "An id is required.")]
    public string Id { get; set; } = "";

    [Required(ErrorMessage = "A name is required.")]
    public string Name { get; set; } = "";

    public string Type { get; set; } = "";
    public int? Stars { get; set; }
    public string Street { get; set; } = "";
    public string Area { get; set; } = "";
    public string City { get; set; } = "";
    public string Postcode { get; set; } = "";
    public string Country { get; set; } = "";
    public double Lat { get; set; }
    public double Lng { get; set; }
    public string Summary { get; set; } = "";
    public string DescriptionText { get; set; } = "";
    public List<RoomFormVm> Rooms { get; set; } = new() { new RoomFormVm() };

    public static StayFormVm From(StayVm stay) => new()
    {
        LockId = true,
        Id = stay.Id,
        Name = stay.Name,
        Type = stay.Type,
        Stars = stay.Stars,
        Street = stay.Street,
        Area = stay.Area,
        City = stay.City,
        Postcode = stay.Postcode,
        Country = stay.Country,
        Lat = stay.Lat,
        Lng = stay.Lng,
        Summary = stay.Summary,
        DescriptionText = AdminText.Join(stay.Description),
        Rooms = stay.Rooms.Count == 0 ? new List<RoomFormVm> { new() } : stay.Rooms
    };

    public StayWriteVm ToWrite(string id) => new()
    {
        Id = id,
        Name = Text(Name),
        Type = Text(Type),
        Stars = Stars,
        Street = Text(Street),
        Area = Text(Area),
        City = Text(City),
        Postcode = Text(Postcode),
        Country = Text(Country),
        Lat = Lat,
        Lng = Lng,
        Summary = Text(Summary),
        Description = AdminText.Paragraphs(DescriptionText),
        Rooms = (Rooms ?? new List<RoomFormVm>())
            .Where(room => !AdminText.BlankRoom(room))
            .Select(room => new RoomFormVm
            {
                Id = Text(room.Id),
                Name = Text(room.Name),
                Price = room.Price,
                Sleeps = room.Sleeps,
                Refundable = room.Refundable,
                Breakfast = room.Breakfast,
                Inventory = room.Inventory
            })
            .ToList()
    };

    private static string Text(string? value) => (value ?? "").Trim();
}

public class StayIndexVm
{
    public string Query { get; set; } = "";
    public IReadOnlyList<StayVm> Items { get; set; } = Array.Empty<StayVm>();
    public string? LoadError { get; set; }
}

public class StayDetailVm
{
    public StayVm Stay { get; set; } = new();
    public IReadOnlyList<PhotoVm> Photos { get; set; } = Array.Empty<PhotoVm>();
}
