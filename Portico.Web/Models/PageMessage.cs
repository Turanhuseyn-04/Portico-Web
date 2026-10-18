namespace Portico.Web.Models;

// A page that cannot go on: what went wrong and where to go instead.
public record PageMessage(string SectionId, string Kicker, string Title, string Text, string LinkHref, string LinkText);
