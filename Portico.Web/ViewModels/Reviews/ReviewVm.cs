using System.ComponentModel.DataAnnotations;

namespace Portico.Web.ViewModels.Reviews;

public class ReviewVm
{
    public int Id { get; set; }
    public string PropertyId { get; set; } = "";
    public string? RoomId { get; set; }
    public string Author { get; set; } = "";
    public string Country { get; set; } = "";
    public string Traveller { get; set; } = "";
    public int Nights { get; set; }
    public DateOnly Date { get; set; }
    public decimal Score { get; set; }
    public string Title { get; set; } = "";
    public string Liked { get; set; } = "";
    public string Disliked { get; set; } = "";
}

public class ReviewFormVm
{
    public int Id { get; set; }

    [Required(ErrorMessage = "A stay id is required.")]
    public string PropertyId { get; set; } = "";

    public string? RoomId { get; set; }

    [Required(ErrorMessage = "An author is required.")]
    public string Author { get; set; } = "";

    public string Country { get; set; } = "";
    public string Traveller { get; set; } = "";
    public int Nights { get; set; }
    public DateOnly Date { get; set; }
    public decimal Score { get; set; }

    [Required(ErrorMessage = "A title is required.")]
    public string Title { get; set; } = "";

    public string Liked { get; set; } = "";
    public string Disliked { get; set; } = "";

    public void Prepare()
    {
        PropertyId = (PropertyId ?? "").Trim();
        RoomId = string.IsNullOrWhiteSpace(RoomId) ? null : RoomId.Trim();
        Author = (Author ?? "").Trim();
        Country = (Country ?? "").Trim();
        Traveller = (Traveller ?? "").Trim();
        Title = (Title ?? "").Trim();
        Liked = (Liked ?? "").Trim();
        Disliked = (Disliked ?? "").Trim();
    }

    public static ReviewFormVm From(ReviewVm review) => new()
    {
        Id = review.Id,
        PropertyId = review.PropertyId,
        RoomId = review.RoomId,
        Author = review.Author,
        Country = review.Country,
        Traveller = review.Traveller,
        Nights = review.Nights,
        Date = review.Date,
        Score = review.Score,
        Title = review.Title,
        Liked = review.Liked,
        Disliked = review.Disliked
    };
}

public class ReviewIndexVm
{
    public string Query { get; set; } = "";
    public IReadOnlyList<ReviewVm> Items { get; set; } = Array.Empty<ReviewVm>();
    public string? LoadError { get; set; }
}
