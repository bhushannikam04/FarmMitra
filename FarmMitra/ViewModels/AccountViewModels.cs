using System.ComponentModel.DataAnnotations;

namespace FarmMitra.ViewModels;

public class LoginViewModel
{
    [Required(ErrorMessage = "Please enter your email or username.")]
    [Display(Name = "Email / Username")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your password.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Display(Name = "Remember me")]
    public bool RememberMe { get; set; }

    public string? ReturnUrl { get; set; }
}

public class RegisterViewModel
{
    [Required]
    public string Role { get; set; } = "Farmer";   // "Farmer" or "Buyer"

    [Required(ErrorMessage = "Please enter your full name.")]
    [StringLength(100)]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = "";

    [Required(ErrorMessage = "Please enter your email.")]
    [EmailAddress(ErrorMessage = "Please enter a valid email address.")]
    public string Email { get; set; } = "";

    [Required(ErrorMessage = "Please enter your mobile number.")]
    [Phone(ErrorMessage = "Please enter a valid mobile number.")]
    [Display(Name = "Mobile Number")]
    public string PhoneNumber { get; set; } = "";

    [Required(ErrorMessage = "Please create a password.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    public string Password { get; set; } = "";

    [Required(ErrorMessage = "Please confirm your password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(Password), ErrorMessage = "Passwords do not match.")]
    [Display(Name = "Confirm Password")]
    public string ConfirmPassword { get; set; } = "";

    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }

    // Farmer-only
    [Display(Name = "Farm Location")]
    public string? FarmLocation { get; set; }

    [Range(0, 100000, ErrorMessage = "Enter a valid farm size.")]
    [Display(Name = "Farm Size (Acres)")]
    public decimal? FarmSizeAcres { get; set; }

    [Display(Name = "Agricultural Products")]
    public string? CropsGrown { get; set; }

    [Range(0, 80, ErrorMessage = "Enter a valid number of years.")]
    [Display(Name = "Experience (Years)")]
    public int? ExperienceYears { get; set; }

    [Range(typeof(bool), "true", "true", ErrorMessage = "You must accept the terms to continue.")]
    public bool AcceptTerms { get; set; }
}
