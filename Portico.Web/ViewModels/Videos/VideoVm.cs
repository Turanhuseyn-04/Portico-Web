using System.ComponentModel.DataAnnotations;

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

    [Required(ErrorMessage = "A video path is required.")]
    public string Src { get; set; } = "";

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
