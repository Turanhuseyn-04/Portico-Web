using System.ComponentModel.DataAnnotations;

namespace Portico.Web.ViewModels.Slides;

public class SlideVm
{
    public int Id { get; set; }
    public string City { get; set; } = "";
    public string Country { get; set; } = "";
    public string Image { get; set; } = "";
    public int Stays { get; set; }
    public int Order { get; set; }
}

public class SlideFormVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "A city is required.")]
    public string City { get; set; } = "";

    [Required(ErrorMessage = "A country is required.")]
    public string Country { get; set; } = "";

    [Required(ErrorMessage = "An image path is required.")]
    public string Image { get; set; } = "";

    public int Stays { get; set; }
    public int Order { get; set; }

    public static SlideFormVm From(SlideVm slide) => new()
    {
        Id = slide.Id,
        City = slide.City,
        Country = slide.Country,
        Image = slide.Image,
        Stays = slide.Stays,
        Order = slide.Order
    };
}

public class SlideIndexVm
{
    public string Query { get; set; } = "";
    public IReadOnlyList<SlideVm> Items { get; set; } = Array.Empty<SlideVm>();
    public string? LoadError { get; set; }
}
