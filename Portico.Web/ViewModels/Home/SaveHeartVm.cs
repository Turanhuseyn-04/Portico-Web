namespace Portico.Web.ViewModels.Home;

// One save button. Saved is null when nobody is signed in.
public record SaveHeartVm(string Id, string Name, bool? Saved, string ReturnUrl, string CssClass);
