using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Web.Models.Accounts;

namespace Portico.Web.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = AccountRoles.Admin)]
    public abstract class AdminControllerBase : Controller
    {
        protected void Page(string title, string description, string skipHref, string skipText, string section)
        {
            ViewData["Title"] = title;
            ViewData["Description"] = description;
            ViewData["BodyClass"] = "admin-shell";
            ViewData["SkipHref"] = skipHref;
            ViewData["SkipText"] = skipText;
            ViewData["Console"] = section;
        }
    }
}
