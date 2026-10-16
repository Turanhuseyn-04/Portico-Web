using Microsoft.AspNetCore.Mvc;

namespace Portico.Web.Areas.Admin.Controllers
{
    public class ReservationController : AdminControllerBase
    {
        public IActionResult Index(string id)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                Page(
                    "Reservations",
                    "Every reservation in this browser, with status, guest, and the amount still due.",
                    "#adminBooks",
                    "Skip to reservations",
                    "Reservations");
                return View("List");
            }

            ViewData["Reference"] = id;
            Page(
                "Reservation",
                "One reservation: guest, stay, dates, cancellation, and total.",
                "#adminReservation",
                "Skip to the reservation",
                "Reservations");
            return View();
        }
    }
}
