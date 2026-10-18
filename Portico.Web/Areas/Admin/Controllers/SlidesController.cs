using Microsoft.AspNetCore.Mvc;
using Portico.Web.Controllers;
using Portico.Web.Helpers;
using Portico.Web.ViewModels.Slides;

namespace Portico.Web.Areas.Admin.Controllers;

public class SlidesController : ApiControllerBase
{
    public SlidesController(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        Page("Slides", "Destination slides: city, country, image path, and how many stays.", "#slidesDesk", "Skip to slides", "Slides");
        var (slides, errors, _) = await GetAsync<List<SlideVm>>("/admin/slides");
        var query = q ?? "";
        var items = (slides ?? new List<SlideVm>())
            .Where(slide => string.IsNullOrWhiteSpace(query)
                || AdminText.Has(slide.City, query)
                || AdminText.Has(slide.Country, query)
                || AdminText.Has(slide.Image, query))
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
    public IActionResult Create()
    {
        Page("New slide", "Add a destination slide.", "#slideForm", "Skip to the form", "Slides");
        return View(new SlideFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(SlideFormVm model)
    {
        Page("New slide", "Add a destination slide.", "#slideForm", "Skip to the form", "Slides");
        if (!ModelState.IsValid) return View(model);

        var (slide, errors, _) = await PostAsync<SlideVm>("/admin/slides", model);
        if (slide is null)
        {
            NoteErrors(errors);
            return View(model);
        }

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
    public async Task<IActionResult> Edit(int id, SlideFormVm model)
    {
        Page("Edit slide", "Change a destination slide.", "#slideForm", "Skip to the form", "Slides");
        model.Id = id;
        if (!ModelState.IsValid) return View(model);

        var (success, errors, status) = await PutAsync($"/admin/slides/{id}", model);
        if (!success)
        {
            if (status == StatusCodes.Status404NotFound)
            {
                TempData["Error"] = "This slide no longer exists.";
                return RedirectToAction(nameof(Index));
            }

            NoteErrors(errors);
            return View(model);
        }

        TempData["Success"] = "The slide was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, errors, status) = await DeleteAsync($"/admin/slides/{id}");
        TempData[success ? "Success" : "Error"] = success
            ? "The slide was deleted."
            : status == StatusCodes.Status404NotFound
                ? "This slide no longer exists."
                : FirstError(errors, "That slide could not be deleted.");
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Missing(int? status, string[] errors)
    {
        if (status == StatusCodes.Status404NotFound) return NotFound();
        TempData["Error"] = FirstError(errors, UnavailableMessage);
        return RedirectToAction(nameof(Index));
    }
}
