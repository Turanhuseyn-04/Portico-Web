using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Portico.Web.ViewModels.Videos;

public class VideoVm
{
    public int Id { get; set; }
    public string Src { get; set; } = "";
    public int Order { get; set; }
}

public class VideoFormVm
{
    public int Id { get; set; }

    // The video file played now. Set by the controller from Portico.Api, never taken from the form.
    [BindNever]
    public string Src { get; set; } = "";

    // The video chosen in the file picker: required for a new video, optional when editing (the current one stays).
    public IFormFile? VideoFile { get; set; }

    public int Order { get; set; }

    public static VideoFormVm From(VideoVm video) => new()
    {
        Id = video.Id,
        Src = video.Src,
        Order = video.Order
    };
}

public class VideoIndexVm
{
    public string Query { get; set; } = "";
    public IReadOnlyList<VideoVm> Items { get; set; } = Array.Empty<VideoVm>();
    public string? LoadError { get; set; }
}
