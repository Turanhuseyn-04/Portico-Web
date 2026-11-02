namespace Portico.Web.Logging;

// What the Web's logs may say about a guest. An email is written as "t***@gmail.com", and an API address loses its
// query string and any email in its path, since both can carry who the guest is. Passwords, API tokens and card
// details are never passed to a logger at all.
public static class LogSafe
{
    public static string Email(string? email)
    {
        var value = (email ?? "").Trim();
        if (value.Length == 0) return "(none)";

        var at = value.LastIndexOf('@');
        if (at <= 0) return "***";
        return $"{value[0]}***{value[at..]}";
    }

    // "/account/turan%40gmail.com/saved?x=1" becomes "/account/t***@gmail.com/saved".
    public static string Url(string? url)
    {
        var path = (url ?? "").Split('?', 2)[0];
        var segments = path.Split('/').Select(segment =>
        {
            var text = Uri.UnescapeDataString(segment);
            return text.Contains('@') ? Email(text) : segment;
        });
        return string.Join("/", segments);
    }
}
