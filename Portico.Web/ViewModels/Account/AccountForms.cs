using System.ComponentModel.DataAnnotations;

namespace Portico.Web.ViewModels.Account;

public class SignInForm
{
    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress(ErrorMessage = "Enter a valid email.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Enter your password.")]
    public string Password { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class RegisterForm
{
    [Required(ErrorMessage = "Enter your full name.")]
    [MinLength(2, ErrorMessage = "Enter your full name.")]
    public string Name { get; set; } = "";

    [Required(ErrorMessage = "Enter your email.")]
    [EmailAddress(ErrorMessage = "Enter a valid email.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Choose a password.")]
    [MinLength(8, ErrorMessage = "Use at least 8 characters.")]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Type the password again.")]
    [Compare(nameof(Password), ErrorMessage = "The passwords do not match.")]
    public string Confirm { get; set; } = "";

    public string? ReturnUrl { get; set; }
}

public class AccountPageVm
{
    public string Mode { get; set; } = "signin";
    public bool Failed { get; set; }
    public string? Error { get; set; }
    public SignInForm SignIn { get; set; } = new();
    public RegisterForm Register { get; set; } = new();

    public bool IsRegister => Mode == "register";
}
