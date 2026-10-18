using Microsoft.AspNetCore.Mvc;
using Portico.Web.Controllers;
using Portico.Web.Helpers;
using Portico.Web.ViewModels.Videos;

namespace Portico.Web.Areas.Admin.Controllers;

public class VideosController : ApiControllerBase
{
    public VideosController(IHttpClientFactory httpClientFactory) : base(httpClientFactory)
    {
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        Page("Videos", "Video paths and the order they play in.", "#videosDesk", "Skip to videos", "Videos");
        var (videos, errors, _) = await GetAsync<List<VideoVm>>("/admin/videos");
        var query = q ?? "";
        var items = (videos ?? new List<VideoVm>())
            .Where(video => string.IsNullOrWhiteSpace(query) || AdminText.Has(video.Src, query))
            .ToList();
        return View(new VideoIndexVm
        {
            Query = query,
            Items = items,
            LoadError = videos is null ? FirstError(errors, UnavailableMessage) : null
        });
    }

    [HttpGet]
    public async Task<IActionResult> Detail(int id)
    {
        Page("Video", "One video path.", "#videoDetail", "Skip to the video", "Videos");
        var (video, errors, status) = await GetAsync<VideoVm>($"/admin/videos/{id}");
        if (video is null)
            return Missing(status, errors);
        return View(video);
    }

    [HttpGet]
    public IActionResult Create()
    {
        Page("New video", "Add a video path.", "#videoForm", "Skip to the form", "Videos");
        return View(new VideoFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(VideoFormVm model)
    {
        Page("New video", "Add a video path.", "#videoForm", "Skip to the form", "Videos");
        if (!ModelState.IsValid) return View(model);

        var (video, errors, _) = await PostAsync<VideoVm>("/admin/videos", model);
        if (video is null)
        {
            NoteErrors(errors);
            return View(model);
        }

        TempData["Success"] = "The video was added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        Page("Edit video", "Change a video path.", "#videoForm", "Skip to the form", "Videos");
        var (video, errors, status) = await GetAsync<VideoVm>($"/admin/videos/{id}");
        if (video is null)
            return Missing(status, errors);
        return View(VideoFormVm.From(video));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, VideoFormVm model)
    {
        Page("Edit video", "Change a video path.", "#videoForm", "Skip to the form", "Videos");
        model.Id = id;
        if (!ModelState.IsValid) return View(model);

        var (success, errors, status) = await PutAsync($"/admin/videos/{id}", model);
        if (!success)
        {
            if (status == StatusCodes.Status404NotFound)
            {
                TempData["Error"] = "This video no longer exists.";
                return RedirectToAction(nameof(Index));
            }

            NoteErrors(errors);
            return View(model);
        }

        TempData["Success"] = "The video was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Delete(int id)
    {
        var (success, errors, status) = await DeleteAsync($"/admin/videos/{id}");
        TempData[success ? "Success" : "Error"] = success
            ? "The video was deleted."
            : status == StatusCodes.Status404NotFound
                ? "This video no longer exists."
                : FirstError(errors, "That video could not be deleted.");
        return RedirectToAction(nameof(Index));
    }

    private IActionResult Missing(int? status, string[] errors)
    {
        if (status == StatusCodes.Status404NotFound) return NotFound();
        TempData["Error"] = FirstError(errors, UnavailableMessage);
        return RedirectToAction(nameof(Index));
    }
}
