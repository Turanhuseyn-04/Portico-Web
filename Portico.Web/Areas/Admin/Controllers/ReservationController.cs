using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Areas.Admin.Controllers
{
    public class ReservationController : AdminControllerBase
    {
        public IActionResult Index(string id)
        {
            ViewData["Reference"] = id;
            Page(
                "Reservation",
                "One reservation for the person who runs the houses: guest, stay, dates, cancellation, and total.",
                "#adminReservation",
                "Skip to the reservation",
                "One reservation, with the guest, the stay, the dates, and what happens to the total.");
            return View();
        }
    }
}
