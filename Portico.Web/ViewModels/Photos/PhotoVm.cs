using System.ComponentModel.DataAnnotations;

namespace Portico.Web.ViewModels.Photos;

public class PhotoVm
{
    public int Id { get; set; }
    public string Src { get; set; } = "";
    public string Alt { get; set; } = "";
    public int Order { get; set; }
}

public class PhotoFormVm
{
    public int Id { get; set; }
    public string PropertyId { get; set; } = "";

    [Required(ErrorMessage = "An image path is required.")]
    public string Src { get; set; } = "";

    public string Alt { get; set; } = "";
    public int Order { get; set; }

    public void Prepare()
    {
        Src = (Src ?? "").Trim();
        Alt = (Alt ?? "").Trim();
    }

    public static PhotoFormVm From(string propertyId, PhotoVm photo) => new()
    {
        Id = photo.Id,
        PropertyId = propertyId,
        Src = photo.Src,
        Alt = photo.Alt,
        Order = photo.Order
    };
}
