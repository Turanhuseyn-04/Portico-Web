using Microsoft.AspNetCore.Mvc;
using Portico.Web.Controllers;
using Portico.Web.Helpers;
using Portico.Web.ViewModels.Reviews;

namespace Portico.Web.Areas.Admin.Controllers;

public class ReviewsController : ApiControllerBase
{
    public ReviewsController(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        Page("Reviews", "The written reviews behind each stay.", "#reviewsDesk", "Skip to reviews", "Reviews");
        var (reviews, errors, _) = await GetAsync<List<ReviewVm>>("/admin/reviews");
        var query = q ?? "";
        var items = (reviews ?? new List<ReviewVm>())
            .Where(review => string.IsNullOrWhiteSpace(query)
                || AdminText.Has(review.Author, query)
                || AdminText.Has(review.Title, query)
                || AdminText.Has(review.PropertyId, query))
            .ToList();
        return View(new ReviewIndexVm
        {
            Query = query,
            Items = items,
            LoadError = reviews is null ? FirstError(errors, UnavailableMessage) : null
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        Page("Review", "One written review.", "#reviewDetail", "Skip to the review", "Reviews");
        var (review, errors, status) = await GetAsync<ReviewVm>($"/admin/reviews/{id}");
        if (review is null)
            return Missing(status, errors);
        return View(review);
    }

    [HttpGet]
    public IActionResult Create()
    {
        Page("New review", "Add a review to a stay.", "#reviewForm", "Skip to the form", "Reviews");
        return View(new ReviewFormVm { Date = DateOnly.FromDateTime(DateTime.Today) });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(ReviewFormVm model)
    {
        Page("New review", "Add a review to a stay.", "#reviewForm", "Skip to the form", "Reviews");
        model.Prepare();
        if (!ModelState.IsValid) return View(model);

        var (review, errors, _) = await PostAsync<ReviewVm>("/admin/reviews", model);
        if (review is null)
        {
            NoteErrors(errors);
            return View(model);
        }

        TempData["Success"] = "The review was added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        Page("Edit review", "Change a written review.", "#reviewForm", "Skip to the form", "Reviews");
        var (review, errors, status) = await GetAsync<ReviewVm>($"/admin/reviews/{id}");
        if (review is null)
            return Missing(status, errors);
        return View(ReviewFormVm.From(review));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, ReviewFormVm model)
    {
        Page("Edit review", "Change a written review.", "#reviewForm", "Skip to the form", "Reviews");
        model.Id = id;
        model.Prepare();
        if (!ModelState.IsValid) return View(model);

        var (success, errors, status) = await PutAsync($"/admin/reviews/{id}", model);
        if (!success)
        {
            if (status == StatusCodes.Status404NotFound)
            {
                TempData["Error"] = "This review no longer exists.";
                return RedirectToAction(nameof(Index));
            }

            NoteErrors(errors);
            return View(model);
        }

        TempData["Success"] = "The review was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, errors, status) = await DeleteAsync($"/admin/reviews/{id}");
        TempData[success ? "Success" : "Error"] = success
            ? "The review was deleted."
            : status == StatusCodes.Status404NotFound
                ? "This review no longer exists."
                : FirstError(errors, "That review could not be deleted.");
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Missing(int? status, string[] errors)
    {
        if (status == StatusCodes.Status404NotFound) return NotFound();
        TempData["Error"] = FirstError(errors, UnavailableMessage);
        return RedirectToAction(nameof(Index));
    }
}
