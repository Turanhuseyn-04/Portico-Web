using Portico.Web.ViewModels.Stays;

namespace Portico.Web.Helpers;

public static class AdminText
{
    public static bool Has(string? value, string query) =>
        !string.IsNullOrWhiteSpace(query)
        && (value ?? "").Contains(query.Trim(), StringComparison.OrdinalIgnoreCase);

    public static List<string> Paragraphs(string? text) =>
        (text ?? "")
            .Replace("\r\n", "\n")
            .Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

    public static string Join(IEnumerable<string>? lines) =>
        string.Join("\n", lines ?? Array.Empty<string>());

    public static bool BlankRoom(RoomFormVm? room) =>
        room is null
        || (string.IsNullOrWhiteSpace(room.Id)
            && string.IsNullOrWhiteSpace(room.Name)
            && room.Price == 0
            && room.Sleeps == 0
            && room.Inventory == 0
            && !room.Refundable
            && !room.Breakfast);
}
