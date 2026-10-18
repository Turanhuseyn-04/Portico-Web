using Microsoft.AspNetCore.Mvc;
using Portico.Web.Models;
using Portico.Web.Models.Accounts;
using Portico.Web.Services;
using Portico.Web.ViewModels.Home;
using System.Diagnostics;

namespace Portico.Web.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly IPropertyApi _properties;
        private readonly IAccountApi _accounts;

        public HomeController(ILogger<HomeController> logger, IPropertyApi properties, IAccountApi accounts)
        {
            _logger = logger;
            _properties = properties;
            _accounts = accounts;
        }

        public async Task<IActionResult> Index(string? sort, string? property, CancellationToken cancellationToken)
        {
            var result = await _properties.ListAsync(cancellationToken);
            var model = HomeVm.Build(result.Data, sort, property);
            if (User.Email() is { } email)
            {
                var saved = await _accounts.SavedAsync(email, cancellationToken);
                model.List.Saved = (saved.Data ?? new List<string>()).ToHashSet();
            }
            return View(model);
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
