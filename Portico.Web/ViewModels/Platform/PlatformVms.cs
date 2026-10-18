using System.ComponentModel.DataAnnotations;

namespace Portico.Web.ViewModels.Platform;

public class AccountRowVm
{
    public string Name { get; set; } = "";
    public string Email { get; set; } = "";
    public string Role { get; set; } = "";
    public bool HasPassword { get; set; }
    public bool EmailConfirmed { get; set; }
}

public class AccountIndexVm
{
    public string Query { get; set; } = "";
    public string Me { get; set; } = "";
    public List<AccountRowVm> Items { get; set; } = new();
    public string? LoadError { get; set; }
}

public class SettingsFormVm
{
    [Required(ErrorMessage = "A phone number is required.")]
    [StringLength(60, ErrorMessage = "Keep the phone number under 60 characters.")]
    public string SupportPhone { get; set; } = "";

    [Required(ErrorMessage = "An email is required.")]
    [EmailAddress(ErrorMessage = "Enter a valid email.")]
    public string SupportEmail { get; set; } = "";

    [Required(ErrorMessage = "A location is required.")]
    [StringLength(120, ErrorMessage = "Keep the location under 120 characters.")]
    public string SupportLocation { get; set; } = "";

    public Dictionary<string, string> ToWrite() => new()
    {
        ["SupportPhone"] = SupportPhone.Trim(),
        ["SupportEmail"] = SupportEmail.Trim(),
        ["SupportLocation"] = SupportLocation.Trim()
    };

    public static SettingsFormVm From(IReadOnlyDictionary<string, string> values) => new()
    {
        SupportPhone = values.GetValueOrDefault("SupportPhone") ?? "",
        SupportEmail = values.GetValueOrDefault("SupportEmail") ?? "",
        SupportLocation = values.GetValueOrDefault("SupportLocation") ?? ""
    };
}

public class AuditEntryVm
{
    public int Id { get; set; }
    public DateTime At { get; set; }
    public string ActorEmail { get; set; } = "";
    public string ActorRole { get; set; } = "";
    public string Method { get; set; } = "";
    public string Path { get; set; } = "";
    public int StatusCode { get; set; }
    public string? Detail { get; set; }

    public bool Refused => StatusCode >= 400;
}

public class AuditIndexVm
{
    public List<AuditEntryVm> Items { get; set; } = new();
    public string? LoadError { get; set; }
}
