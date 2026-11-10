using Microsoft.AspNetCore.Mvc;
using Portico.Web.Services;
using Portico.Web.ViewModels.Home;
using Portico.Web.ViewModels.Property;
using Portico.Web.ViewModels.Videos;

namespace Portico.Web.ViewComponents
{
    public class SiteHeaderViewComponent : ViewComponent
    {
        public IViewComponentResult Invoke() => View();
    }

    // The contact details come from the site settings a SuperAdmin can change.
    public class SiteFooterViewComponent : ViewComponent
    {
        private readonly ISettingsApi _settings;

        public SiteFooterViewComponent(ISettingsApi settings)
        {
            _settings = settings;
        }

        public async Task<IViewComponentResult> InvokeAsync() => View(await _settings.GetAsync(HttpContext.RequestAborted));
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

    // The videos an admin manages. The two the site shipped with stand in when Portico.Api cannot be reached.
    public class VideoBandViewComponent : ViewComponent
    {
        private static readonly IReadOnlyList<VideoVm> Fallback = new[]
        {
            new VideoVm { Id = 1, Src = "/assets/videos/villas/14041585_1920_1080_25fps.mp4", Order = 0 },
            new VideoVm { Id = 2, Src = "/assets/videos/resorts/3576398-uhd_3840_2160_25fps.mp4", Order = 1 }
        };

        private readonly IContentApi _content;

        public VideoBandViewComponent(IContentApi content)
        {
            _content = content;
        }

        public async Task<IViewComponentResult> InvokeAsync() =>
            View(await _content.VideosAsync(HttpContext.RequestAborted) ?? Fallback);
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
