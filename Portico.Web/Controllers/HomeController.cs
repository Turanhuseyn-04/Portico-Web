using Microsoft.AspNetCore.Mvc;
using Portico.Web.Models;
using System.Diagnostics;

namespace Portico.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;

        public HomeController(ILogger<HomeController> logger)
        {
            _logger = logger;
        }

        public IActionResult Index()
        {
            return View();
        }

        public IActionResult Privacy()
        {
            return View();
        }

        public IActionResult Terms()
        {
            return View();
        }

        public IActionResult Help()
        {
            return View();
        }

        public IActionResult Search()
        {
            return View();
        }

        public IActionResult Destinations()
        {
            return View();
        }

        public IActionResult Types()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
