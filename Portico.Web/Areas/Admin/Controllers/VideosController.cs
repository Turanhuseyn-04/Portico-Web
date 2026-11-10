using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Portico.Web.Controllers;
using Portico.Web.Models.Accounts;
using Portico.Web.Helpers;
using Portico.Web.Services;
using Portico.Web.ViewModels.Videos;

namespace Portico.Web.Areas.Admin.Controllers;

// The home page's video band. The video is uploaded with a file picker and saved by MediaStore.
public class VideosController : ApiControllerBase
{
    private const long MaxRequestBytes = MediaStore.MaxVideoBytes + 1024 * 1024;

    private readonly IMediaStore _media;
    private readonly IContentApi _content;

    public VideosController(IHttpClientFactory httpClientFactory, IMediaStore media, IContentApi content) : base(httpClientFactory)
    {
        _media = media;
        _content = content;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? q)
    {
        Page("Videos", "The videos on the home page and the order they play in.", "#videosDesk", "Skip to videos", "Videos");
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
        Page("Video", "One home page video.", "#videoDetail", "Skip to the video", "Videos");
        var (video, errors, status) = await GetAsync<VideoVm>($"/admin/videos/{id}");
        if (video is null)
            return Missing(status, errors);
        return View(video);
    }

    [HttpGet]
    [Authorize(Policy = AccountPolicies.SuperAdmin)]
    public IActionResult Create()
    {
        Page("New video", "Upload a video for the home page.", "#videoForm", "Skip to the form", "Videos");
        return View(new VideoFormVm());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AccountPolicies.SuperAdmin)]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Create(VideoFormVm model, CancellationToken cancellationToken)
    {
        Page("New video", "Upload a video for the home page.", "#videoForm", "Skip to the form", "Videos");
        if (!ModelState.IsValid) return View(model);

        var saved = await _media.SaveAsync(model.VideoFile, MediaKind.Video, cancellationToken);
        if (saved.Error is not null)
        {
            ModelState.AddModelError(nameof(model.VideoFile), saved.Error);
            return View(model);
        }

        var (video, errors, _) = await PostAsync<VideoVm>("/admin/videos", Body(model, saved.Path!));
        if (video is null)
        {
            _media.DeleteIfUploaded(saved.Path);
            NoteErrors(errors);
            return View(model);
        }

        _content.Forget();
        TempData["Success"] = "The video was added.";
        return RedirectToAction(nameof(Index));
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id)
    {
        Page("Edit video", "Replace a video or change its place.", "#videoForm", "Skip to the form", "Videos");
        var (video, errors, status) = await GetAsync<VideoVm>($"/admin/videos/{id}");
        if (video is null)
            return Missing(status, errors);
        return View(VideoFormVm.From(video));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [RequestSizeLimit(MaxRequestBytes)]
    [RequestFormLimits(MultipartBodyLengthLimit = MaxRequestBytes)]
    public async Task<IActionResult> Edit(int id, VideoFormVm model, CancellationToken cancellationToken)
    {
        Page("Edit video", "Replace a video or change its place.", "#videoForm", "Skip to the form", "Videos");
        model.Id = id;

        // The current file comes from the API, so the form cannot point the video (or the clean-up) at another file.
        var (current, loadErrors, status) = await GetAsync<VideoVm>($"/admin/videos/{id}");
        if (current is null) return Gone(status, loadErrors, "This video no longer exists.");
        model.Src = current.Src;
        if (!ModelState.IsValid) return View(model);

        string? uploaded = null;
        if (model.VideoFile is { Length: > 0 })
        {
            var saved = await _media.SaveAsync(model.VideoFile, MediaKind.Video, cancellationToken);
            if (saved.Error is not null)
            {
                ModelState.AddModelError(nameof(model.VideoFile), saved.Error);
                return View(model);
            }
            uploaded = saved.Path;
        }

        var (success, errors, putStatus) = await PutAsync($"/admin/videos/{id}", Body(model, uploaded ?? current.Src));
        if (!success)
        {
            _media.DeleteIfUploaded(uploaded);
            if (putStatus == StatusCodes.Status404NotFound) return Gone(putStatus, errors, "This video no longer exists.");
            NoteErrors(errors);
            return View(model);
        }

        if (uploaded is not null) _media.DeleteIfUploaded(current.Src);
        _content.Forget();
        TempData["Success"] = "The video was updated.";
        return RedirectToAction(nameof(Index));
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    [Authorize(Policy = AccountPolicies.SuperAdmin)]
    public async Task<IActionResult> Delete(int id)
    {
        var (current, _, _) = await GetAsync<VideoVm>($"/admin/videos/{id}");
        var (success, errors, status) = await DeleteAsync($"/admin/videos/{id}");
        if (success)
        {
            _media.DeleteIfUploaded(current?.Src);
            _content.Forget();
        }

        TempData[success ? "Success" : "Error"] = success
            ? "The video was deleted."
            : status == StatusCodes.Status404NotFound
                ? "This video no longer exists."
                : FirstError(errors, "That video could not be deleted.");
        return RedirectToAction(nameof(Index));
    }

    private static object Body(VideoFormVm model, string src) => new { src, order = model.Order };

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
