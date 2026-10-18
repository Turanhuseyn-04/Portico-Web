using Microsoft.AspNetCore.Mvc;
using Portico.Web.ViewModels.Home;
using Portico.Web.ViewModels.Property;

namespace Portico.Web.ViewComponents
{
    public class SiteHeaderViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }

    public class SiteFooterViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }

    public class AuthDialogViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }

    public class StayDrawerViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }

    public class HeroSearchViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(HeroSearchVm model) => View(model);
    }

    public class TrendingDestinationsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(List<DestinationVm> model) => View(model);
    }

    public class HomePropertyTypesViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }

    public class StayRulesViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }

    public class StayListViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(StayListVm model) => View(model);
    }

    public class VideoBandViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }

    public class GuestNotesViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }

    public class CloseBandViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }

    public class PropertyHeadViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PropertyPageVm model) => View(model);
    }

    public class PropertyGalleryViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PropertyPageVm model) => View(model);
    }

    public class PropertySubnavViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }

    public class BookingCardViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PropertyPageVm model) => View(model);
    }

    public class PropertyAboutViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PropertyPageVm model) => View(model);
    }

    public class PropertyAmenitiesViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PropertyPageVm model) => View(model);
    }

    public class PropertyRoomsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PropertyPageVm model) => View(model);
    }

    public class PropertyReviewsViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PropertyPageVm model) => View(model);
    }

    public class PropertyLocationViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PropertyPageVm model) => View(model);
    }

    public class PropertyHouseRulesViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PropertyPageVm model) => View(model);
    }

    public class PropertyFaqViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PropertyPageVm model) => View(model);
    }

    public class MobileReserveBarViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PropertyPageVm model) => View(model);
    }

    public class PropertyLightboxViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke(PropertyPageVm model) => View(model);
    }
}
