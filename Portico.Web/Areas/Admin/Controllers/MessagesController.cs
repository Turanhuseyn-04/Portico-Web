using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Areas.Admin.Controllers
{
    public class MessagesController : AdminControllerBase
    {
        public IActionResult Index()
        {
            Page(
                "Messages",
                "Notes sent from Help and kept in this browser.",
                "#adminMessages",
                "Skip to messages",
                "Messages");
            return View();
        }
    }
}
