using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Areas.Admin.Controllers
{
    public class ReviewsController : AdminControllerBase
    {
        public IActionResult Index(string id)
        {
            ViewData["StayId"] = id;
            Page(
                "Reviews",
                "The written reviews behind each stay's score.",
                "#adminReviews",
                "Skip to reviews",
                "Reviews");
            return View();
        }
    }
}
