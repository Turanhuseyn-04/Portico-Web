using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Areas.Admin.Controllers
{
    public class UsersController : AdminControllerBase
    {
        public IActionResult Index()
        {
            Page(
                "Users",
                "The account in this browser and the guests named on reservations. Passwords are not stored.",
                "#adminUsers",
                "Skip to users",
                "Users");
            return View();
        }
    }
}
