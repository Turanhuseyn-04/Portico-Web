using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    public abstract class AdminControllerBase : Controller
    {
        protected void Page(string title, string description, string skipHref, string skipText, string footerNote)
        {
            ViewData["Title"] = title;
            ViewData["Description"] = description;
            ViewData["BodyClass"] = "results-page";
            ViewData["SkipHref"] = skipHref;
            ViewData["SkipText"] = skipText;
            ViewData["FooterNote"] = footerNote;
        }
    }
}
