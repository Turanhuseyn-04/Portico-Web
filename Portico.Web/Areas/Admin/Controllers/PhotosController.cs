using Microsoft.AspNetCore.Mvc;
using Portico.Web.Controllers;
using Portico.Web.ViewModels.Photos;
using Portico.Web.ViewModels.Stays;

namespace Portico.Web.Areas.Admin.Controllers;

public class PhotosController : ApiControllerBase
{
    public PhotosController(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Create(string propertyId)
    {
        Page("New photo", "Add a photograph to this stay.", "#photoForm", "Skip to the form", "Stays");
        if (!await StayExists(propertyId)) return NotFound();
        return View(new PhotoFormVm { PropertyId = propertyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(string propertyId, PhotoFormVm model)
    {
        Page("New photo", "Add a photograph to this stay.", "#photoForm", "Skip to the form", "Stays");
        model.PropertyId = propertyId;
        if (string.IsNullOrWhiteSpace(propertyId)) return NotFound();
        model.Prepare();
        if (!ModelState.IsValid) return View(model);

        var (photo, errors, status) = await PostAsync<PhotoVm>(CollectionUrl(propertyId), model);
        if (photo is null)
        {
            if (status == StatusCodes.Status404NotFound) return NotFound();
            NoteErrors(errors);
            return View(model);
        }

        TempData["Success"] = "The photo was added.";
        return RedirectToAction("Detail", "Stay", new { id = propertyId });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string propertyId, int id)
    {
        Page("Edit photo", "Change the path, alt text, or order.", "#photoForm", "Skip to the form", "Stays");
        var (photo, errors, status) = await GetAsync<PhotoVm>($"{CollectionUrl(propertyId)}/{id}");
        if (photo is null)
        {
            if (status == StatusCodes.Status404NotFound) return NotFound();
            TempData["Error"] = FirstError(errors, UnavailableMessage);
            return RedirectToAction("Detail", "Stay", new { id = propertyId });
        }
        return View(PhotoFormVm.From(propertyId, photo));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string propertyId, int id, PhotoFormVm model)
    {
        Page("Edit photo", "Change the path, alt text, or order.", "#photoForm", "Skip to the form", "Stays");
        model.PropertyId = propertyId;
        model.Id = id;
        model.Prepare();
        if (!ModelState.IsValid) return View(model);

        var (success, errors, status) = await PutAsync($"{CollectionUrl(propertyId)}/{id}", model);
        if (!success)
        {
            if (status == StatusCodes.Status404NotFound) return NotFound();
            NoteErrors(errors);
            return View(model);
        }

        TempData["Success"] = "The photo was updated.";
        return RedirectToAction("Detail", "Stay", new { id = propertyId });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string propertyId, int id)
    {
        var (success, errors, status) = await DeleteAsync($"{CollectionUrl(propertyId)}/{id}");
        if (status == StatusCodes.Status404NotFound && !success)
            return NotFound();

        TempData[success ? "Success" : "Error"] = success
            ? "The photo was deleted."
            : FirstError(errors, "That photo could not be deleted.");
        return RedirectToAction("Detail", "Stay", new { id = propertyId });
    }

    private async Task<bool> StayExists(string propertyId)
    {
        if (string.IsNullOrWhiteSpace(propertyId)) return false;
        var (stay, _, status) = await GetAsync<StayVm>($"/admin/properties/{Uri.EscapeDataString(propertyId)}");
        return stay is not null && status == StatusCodes.Status200OK;
    }

    private static string CollectionUrl(string propertyId) =>
        $"/admin/properties/{Uri.EscapeDataString(propertyId)}/photos";
}
