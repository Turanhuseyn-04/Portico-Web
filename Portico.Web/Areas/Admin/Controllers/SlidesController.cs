using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Web.Controllers;
using Portico.Web.Models.Accounts;
using Portico.Web.Helpers;
using Portico.Web.Services;
using Portico.Web.ViewModels.Slides;

namespace Portico.Web.Areas.Admin.Controllers;

// The home page's trending destinations. The image is uploaded with a file picker and saved by MediaStore.
public class SlidesController : ApiControllerBase
{
    private const long MaxRequestBytes = MediaStore.MaxImageBytes + 1024 * 1024;

    private readonly IMediaStore _media;
    private readonly IContentApi _content;

    public SlidesController(IHttpClientFactory httpClientFactory, IMediaStore media, IContentApi content) : base(httpClientFactory)
    {
        _media = media;
        _content = content;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        Page("Slides", "The trending destinations on the home page: city, country, image, and how many stays.", "#slidesDesk", "Skip to slides", "Slides");
        var (slides, errors, _) = await GetAsync<List<SlideVm>>("/admin/slides");
        var query = q ?? "";
        var items = (slides ?? new List<SlideVm>())
            .Where(slide => string.IsNullOrWhiteSpace(query)
                || AdminText.Has(slide.City, query)
                || AdminText.Has(slide.Country, query))
            .ToList();
        return View(new SlideIndexVm
        {
            Query = query,
            Items = items,
            LoadError = slides is null ? FirstError(errors, UnavailableMessage) : null
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        Page("Slide", "One destination slide.", "#slideDetail", "Skip to the slide", "Slides");
        var (slide, errors, status) = await GetAsync<SlideVm>($"/admin/slides/{id}");
        if (slide is null)
            return Missing(status, errors);
        return View(slide);
    }

    [HttpGet]
    [Authorize(Policy = AccountPolicies.SuperAdmin)]
    public IActionResult Create()
    {
        Page("New slide", "Add a destination slide.", "#slideForm", "Skip to the form", "Slides");
        return View(new SlideFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AccountPolicies.SuperAdmin)]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Create(SlideFormVm model, CancellationToken cancellationToken)
    {
        Page("New slide", "Add a destination slide.", "#slideForm", "Skip to the form", "Slides");
        if (!ModelState.IsValid) return View(model);

        var saved = await _media.SaveAsync(model.ImageFile, MediaKind.SlideImage, cancellationToken);
        if (saved.Error is not null)
        {
            ModelState.AddModelError(nameof(model.ImageFile), saved.Error);
            return View(model);
        }

        var (slide, errors, _) = await PostAsync<SlideVm>("/admin/slides", Body(model, saved.Path!));
        if (slide is null)
        {
            _media.DeleteIfUploaded(saved.Path);
            NoteErrors(errors);
            return View(model);
        }

        _content.Forget();
        TempData["Success"] = "The slide was added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        Page("Edit slide", "Change a destination slide.", "#slideForm", "Skip to the form", "Slides");
        var (slide, errors, status) = await GetAsync<SlideVm>($"/admin/slides/{id}");
        if (slide is null)
            return Missing(status, errors);
        return View(SlideFormVm.From(slide));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Edit(int id, SlideFormVm model, CancellationToken cancellationToken)
    {
        Page("Edit slide", "Change a destination slide.", "#slideForm", "Skip to the form", "Slides");
        model.Id = id;

        // The current image comes from the API, so the form cannot point the slide (or the clean-up) at another file.
        var (current, loadErrors, status) = await GetAsync<SlideVm>($"/admin/slides/{id}");
        if (current is null) return Gone(status, loadErrors, "This slide no longer exists.");
        model.Image = current.Image;
        if (!ModelState.IsValid) return View(model);

        string? uploaded = null;
        if (model.ImageFile is { Length: > 0 })
        {
            var saved = await _media.SaveAsync(model.ImageFile, MediaKind.SlideImage, cancellationToken);
            if (saved.Error is not null)
            {
                ModelState.AddModelError(nameof(model.ImageFile), saved.Error);
                return View(model);
            }
            uploaded = saved.Path;
        }

        var (success, errors, putStatus) = await PutAsync($"/admin/slides/{id}", Body(model, uploaded ?? current.Image));
        if (!success)
        {
            _media.DeleteIfUploaded(uploaded);
            if (putStatus == StatusCodes.Status404NotFound) return Gone(putStatus, errors, "This slide no longer exists.");
            NoteErrors(errors);
            return View(model);
        }

        if (uploaded is not null) _media.DeleteIfUploaded(current.Image);
        _content.Forget();
        TempData["Success"] = "The slide was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AccountPolicies.SuperAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var (current, _, _) = await GetAsync<SlideVm>($"/admin/slides/{id}");
        var (success, errors, status) = await DeleteAsync($"/admin/slides/{id}");
        if (success)
        {
            _media.DeleteIfUploaded(current?.Image);
            _content.Forget();
        }

        TempData[success ? "Success" : "Error"] = success
            ? "The slide was deleted."
            : status == StatusCodes.Status404NotFound
                ? "This slide no longer exists."
                : FirstError(errors, "That slide could not be deleted.");
        return RedirectToAction(nameof(Index));
    }

    private static object Body(SlideFormVm model, string image) => new
    {
        city = model.City,
        country = model.Country,
        image,
        stays = model.Stays,
        order = model.Order
    };

    private IActionResult Gone(int? status, string[] errors, string missing)
    {
        TempData["Error"] = status == StatusCodes.Status404NotFound ? missing : FirstError(errors, UnavailableMessage);
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Missing(int? status, string[] errors)
    {
        if (status == StatusCodes.Status404NotFound) return NotFound();
        TempData["Error"] = FirstError(errors, UnavailableMessage);
        return RedirectToAction(nameof(Index));
    }
}
