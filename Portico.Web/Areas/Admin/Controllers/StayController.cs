using Microsoft.AspNetCore.Mvc;
using Portico.Web.Controllers;
using Portico.Web.Helpers;
using Portico.Web.ViewModels.Photos;
using Portico.Web.ViewModels.Stays;

namespace Portico.Web.Areas.Admin.Controllers;

public class StayController : ApiControllerBase
{
    public StayController(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        Page("Stays", "Add a stay, open one to edit its rooms and photographs, or delete one that has no reservation.", "#staysDesk", "Skip to stays", "Stays");
        var (stays, errors, _) = await GetAsync<List<StayVm>>("/admin/properties");
        var query = q ?? "";
        var items = (stays ?? new List<StayVm>())
            .Where(stay => string.IsNullOrWhiteSpace(query)
                || AdminText.Has(stay.Id, query)
                || AdminText.Has(stay.Name, query)
                || AdminText.Has(stay.City, query))
            .ToList();
        return View(new StayIndexVm
        {
            Query = query,
            Items = items,
            LoadError = stays is null ? FirstError(errors, UnavailableMessage) : null
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(string id)
    {
        Page("Stay", "One stay: address, rooms, and photographs.", "#stayDetail", "Skip to the stay", "Stays");
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var (stay, errors, status) = await GetAsync<StayVm>(PropertyUrl(id));
        if (stay is null)
            return Missing(status, errors);

        var (photos, _, photoStatus) = await GetAsync<List<PhotoVm>>(PhotosUrl(id));
        return View(new StayDetailVm
        {
            Stay = stay,
            Photos = photoStatus == StatusCodes.Status404NotFound ? Array.Empty<PhotoVm>() : photos ?? new List<PhotoVm>()
        });
    }

    [HttpGet]
    public IActionResult Create()
    {
        Page("New stay", "Add a stay. The id is a short slug, such as marble.", "#stayForm", "Skip to the form", "Stays");
        return View(new StayFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(StayFormVm model)
    {
        Page("New stay", "Add a stay. The id is a short slug, such as marble.", "#stayForm", "Skip to the form", "Stays");
        Prepare(model, lockId: false);
        model.Id = (model.Id ?? "").Trim();
        if (!TryValidateStay(model, model.Id)) return View(model);

        var slug = model.Id;
        var (stay, errors, _) = await PostAsync<StayVm>("/admin/properties", model.ToWrite(slug));
        if (stay is null)
        {
            NoteErrors(errors);
            return View(model);
        }

        TempData["Success"] = "The stay was added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(string id)
    {
        Page("Edit stay", "Change a stay. The id stays the same.", "#stayForm", "Skip to the form", "Stays");
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        var (stay, errors, status) = await GetAsync<StayVm>(PropertyUrl(id));
        if (stay is null)
            return Missing(status, errors);
        return View(StayFormVm.From(stay));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(string id, StayFormVm model)
    {
        Page("Edit stay", "Change a stay. The id stays the same.", "#stayForm", "Skip to the form", "Stays");
        if (string.IsNullOrWhiteSpace(id)) return NotFound();

        model.Id = id;
        Prepare(model, lockId: true);
        if (!TryValidateStay(model, id)) return View(model);

        var (success, errors, status) = await PutAsync(PropertyUrl(id), model.ToWrite(id));
        if (!success)
        {
            if (status == StatusCodes.Status404NotFound)
            {
                TempData["Error"] = "This stay no longer exists.";
                return RedirectToAction(nameof(Index));
            }

            NoteErrors(errors);
            return View(model);
        }

        TempData["Success"] = "The stay was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(string id)
    {
        var (success, errors, status) = await DeleteAsync(PropertyUrl(id));
        TempData[success ? "Success" : "Error"] = success
            ? "The stay was deleted."
            : FirstError(errors, status == StatusCodes.Status404NotFound
                ? "This stay no longer exists."
                : "That stay could not be deleted.");
        return RedirectToAction(nameof(Index));
    }

    private bool TryValidateStay(StayFormVm model, string? id)
    {
        if (string.IsNullOrWhiteSpace(id))
            ModelState.AddModelError(nameof(model.Id), "An id is required.");
        else if (id.Any(char.IsWhiteSpace))
            ModelState.AddModelError(nameof(model.Id), "The id is a short slug with no spaces.");

        if (model.Rooms.Any(room => !AdminText.BlankRoom(room) && string.IsNullOrWhiteSpace(room.Id)))
            ModelState.AddModelError(string.Empty, "Each room needs an id.");

        return ModelState.IsValid;
    }

    private static void Prepare(StayFormVm model, bool lockId)
    {
        model.LockId = lockId;
        model.Rooms ??= new List<RoomFormVm>();
        if (model.Rooms.Count == 0)
            model.Rooms.Add(new RoomFormVm());
    }

    private IActionResult Missing(int? status, string[] errors)
    {
        if (status == StatusCodes.Status404NotFound) return NotFound();
        TempData["Error"] = FirstError(errors, UnavailableMessage);
        return RedirectToAction(nameof(Index));
    }

    private static string PropertyUrl(string id) => $"/admin/properties/{Uri.EscapeDataString(id)}";

    private static string PhotosUrl(string id) => $"/admin/properties/{Uri.EscapeDataString(id)}/photos";
}
