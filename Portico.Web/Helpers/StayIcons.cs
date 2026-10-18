using Microsoft.AspNetCore.Html;

namespace Portico.Web.Helpers;

// Line icons for facilities and highlights. The API sends the key; the label and drawing live here.
public static class StayIcons
{
    private static readonly Dictionary<string, (string Label, string Path)> Icons = new()
    {
        ["wifi"] = ("Free WiFi", "<path d=\"M3 9a14 14 0 0 1 18 0M6 12.5a9 9 0 0 1 12 0M9 16a4 4 0 0 1 6 0\"/><circle cx=\"12\" cy=\"19.2\" r=\"0.9\" fill=\"currentColor\"/>"),
        ["breakfast"] = ("Breakfast", "<path d=\"M4 10h13v3a6 6 0 0 1-6 6h-1a6 6 0 0 1-6-6zM17 11h1.5a2.5 2.5 0 0 1 0 5H16M8 3.5v3.5M12 3.5v3.5\"/>"),
        ["ac"] = ("Air conditioning", "<path d=\"M12 3v18M4.2 7.5l15.6 9M4.2 16.5l15.6-9M9.5 4.5 12 7l2.5-2.5M9.5 19.5 12 17l2.5 2.5\"/>"),
        ["terrace"] = ("Terrace", "<path d=\"M3 12a9 9 0 0 1 18 0zM12 12v8M8 20h8\"/>"),
        ["bar"] = ("Bar", "<path d=\"M5 4h14l-7 8zM12 12v8M8 20h8\"/>"),
        ["desk"] = ("24-hour front desk", "<path d=\"M4 18h16M6 18a6 6 0 0 1 12 0M12 9V7M10 7h4\"/>"),
        ["elevator"] = ("Lift", "<rect x=\"5\" y=\"3\" width=\"14\" height=\"18\" rx=\"2\"/><path d=\"M9 9.5l3-3 3 3M9 14.5l3 3 3-3\"/>"),
        ["nonsmoking"] = ("Non-smoking rooms", "<circle cx=\"12\" cy=\"12\" r=\"9\"/><path d=\"M5.6 5.6l12.8 12.8M7 13h6M15 13h2\"/>"),
        ["garden"] = ("Garden", "<path d=\"M12 21v-8M12 13c-4 0-6-3-6-7 4 0 6 3 6 7zM12 13c4 0 6-3 6-7-4 0-6 3-6 7zM6 21h12\"/>"),
        ["bath"] = ("Bathtub", "<path d=\"M3 12h18v2a5 5 0 0 1-5 5H8a5 5 0 0 1-5-5zM6 12V6a2 2 0 0 1 4 0M7 19l-1 2M17 19l1 2\"/>"),
        ["kitchen"] = ("Kitchen", "<rect x=\"4\" y=\"3\" width=\"16\" height=\"18\" rx=\"2\"/><path d=\"M4 9h16M8 6h.01M11 6h.01M9 13h6v4H9z\"/>"),
        ["workspace"] = ("Workspace", "<rect x=\"3\" y=\"5\" width=\"18\" height=\"11\" rx=\"1.5\"/><path d=\"M8 20h8M12 16v4\"/>"),
        ["washer"] = ("Washing machine", "<rect x=\"4\" y=\"3\" width=\"16\" height=\"18\" rx=\"2\"/><circle cx=\"12\" cy=\"13\" r=\"4.5\"/><path d=\"M8 6.5h.01M11 6.5h3\"/>"),
        ["bike"] = ("Bicycles", "<circle cx=\"6\" cy=\"16\" r=\"3.5\"/><circle cx=\"18\" cy=\"16\" r=\"3.5\"/><path d=\"M6 16l4-7h5l3 7M10 9l2 7M14 6h2\"/>"),
        ["pool"] = ("Outdoor pool", "<path d=\"M3 18c1.5 1 3 1 4.5 0s3-1 4.5 0 3 1 4.5 0 3-1 4.5 0M8 15V5.5a2 2 0 0 1 4 0M16 15V5.5a2 2 0 0 0-4 0M8 9h8M8 12.5h8\"/>"),
        ["spa"] = ("Spa", "<path d=\"M12 20c-5 0-8-3-8-7 3 0 6 1 8 4 2-3 5-4 8-4 0 4-3 7-8 7zM12 17c-2-3-2-7 0-11 2 4 2 8 0 11z\"/>"),
        ["restaurant"] = ("Restaurant", "<path d=\"M7 3v8M5 3v5a2 2 0 0 0 4 0V3M7 11v10M17 3c-2 1-3 4-3 7h3v11\"/>"),
        ["view"] = ("Sea view", "<path d=\"M3 16c2 1.5 4 1.5 6 0s4-1.5 6 0 4 1.5 6 0M3 20c2 1.5 4 1.5 6 0s4-1.5 6 0 4 1.5 6 0\"/><circle cx=\"12\" cy=\"8\" r=\"3\"/>"),
        ["parking"] = ("Parking", "<rect x=\"4\" y=\"3\" width=\"16\" height=\"18\" rx=\"3\"/><path d=\"M10 17V7h3a3 3 0 0 1 0 6h-3\"/>"),
        ["shuttle"] = ("Airport shuttle", "<rect x=\"3\" y=\"5\" width=\"18\" height=\"11\" rx=\"2\"/><path d=\"M3 11h18\"/><circle cx=\"7.5\" cy=\"18.5\" r=\"1.5\"/><circle cx=\"16.5\" cy=\"18.5\" r=\"1.5\"/>"),
        ["family"] = ("Family friendly", "<circle cx=\"8\" cy=\"7\" r=\"2.5\"/><circle cx=\"16.5\" cy=\"9\" r=\"2\"/><path d=\"M3.5 20a4.5 4.5 0 0 1 9 0M13 20a3.5 3.5 0 0 1 7 0\"/>"),
        ["fireplace"] = ("Fireplace", "<path d=\"M3 4h18v3H3zM5 7v13h14V7M12 19c-2 0-3-1.3-3-3 0-2 3-3.5 3-6 1.5 1.5 3 3.5 3 6 0 1.7-1 3-3 3z\"/>"),
        ["pin"] = ("Location", "<path d=\"M12 21s-6-5.5-6-11a6 6 0 0 1 12 0c0 5.5-6 11-6 11z\"/><circle cx=\"12\" cy=\"10\" r=\"2.2\"/>"),
        ["home"] = ("Whole place", "<path d=\"M3 11l9-7 9 7M5 9.5V20h14V9.5M10 20v-6h4v6\"/>"),
        ["grid"] = ("Photos", "<rect x=\"3\" y=\"3\" width=\"7.5\" height=\"7.5\" rx=\"1.5\"/><rect x=\"13.5\" y=\"3\" width=\"7.5\" height=\"7.5\" rx=\"1.5\"/><rect x=\"3\" y=\"13.5\" width=\"7.5\" height=\"7.5\" rx=\"1.5\"/><rect x=\"13.5\" y=\"13.5\" width=\"7.5\" height=\"7.5\" rx=\"1.5\"/>"),
        ["check"] = ("Included", "<path d=\"M5 12.5l4.5 4.5L19 7.5\"/>"),
        ["guest"] = ("Guests", "<circle cx=\"12\" cy=\"8\" r=\"3.5\"/><path d=\"M5 20a7 7 0 0 1 14 0\"/>")
    };

    public static bool Has(string key) => Icons.ContainsKey(key);

    public static string Label(string key) => Icons.TryGetValue(key, out var icon) ? icon.Label : key;

    public static IHtmlContent Svg(string key, int size = 22)
    {
        var path = Icons.TryGetValue(key, out var icon) ? icon.Path : Icons["check"].Path;
        return new HtmlString(
            $"<svg class=\"ico\" viewBox=\"0 0 24 24\" width=\"{size}\" height=\"{size}\" aria-hidden=\"true\" fill=\"none\" stroke=\"currentColor\" stroke-width=\"1.5\" stroke-linecap=\"round\" stroke-linejoin=\"round\">{path}</svg>");
    }
}
