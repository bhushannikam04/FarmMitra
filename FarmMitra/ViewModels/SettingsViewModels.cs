using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace FarmMitra.ViewModels;

public class ProfileViewModel
{
    [Required(ErrorMessage = "Enter your full name.")]
    [StringLength(100, ErrorMessage = "Keep your name under 100 characters.")]
    [Display(Name = "Full Name")]
    public string FullName { get; set; } = "";

    [Phone(ErrorMessage = "Enter a valid mobile number.")]
    [StringLength(20)]
    [Display(Name = "Mobile Number")]
    public string? Phone { get; set; }

    [StringLength(250, ErrorMessage = "Keep the address under 250 characters.")]
    public string? Address { get; set; }

    [StringLength(80)]
    public string? City { get; set; }

    [StringLength(80)]
    public string? State { get; set; }

    // Farmer-only
    [StringLength(150)]
    [Display(Name = "Farm Location")]
    public string? FarmLocation { get; set; }

    [StringLength(250)]
    [Display(Name = "Crops Grown")]
    public string? CropsGrown { get; set; }

    [Range(0.1, 100000, ErrorMessage = "Enter a farm size between 0.1 and 100000 acres.")]
    [Display(Name = "Farm Size (acres)")]
    public decimal? FarmSizeAcres { get; set; }

    [Range(0, 80, ErrorMessage = "Enter experience between 0 and 80 years.")]
    [Display(Name = "Experience (years)")]
    public int? ExperienceYears { get; set; }

    // Profile photo
    [Display(Name = "Profile Photo")]
    public IFormFile? Photo { get; set; }
    public bool RemovePhoto { get; set; }

    // Read-only details (shown, never trusted from the form)
    public string? ProfileImagePath { get; set; }
    // Nullable on purpose: these are not posted, and non-nullable strings would be treated as [Required].
    public string? Email { get; set; }
    public string? Role { get; set; }
    public DateTime MemberSince { get; set; }
    public string? ApprovalStatus { get; set; }
}

public class ChangePasswordViewModel
{
    [Required(ErrorMessage = "Enter your current password.")]
    [DataType(DataType.Password)]
    [Display(Name = "Current Password")]
    public string CurrentPassword { get; set; } = "";

    [Required(ErrorMessage = "Enter a new password.")]
    [StringLength(100, MinimumLength = 6, ErrorMessage = "The new password must be at least 6 characters.")]
    [DataType(DataType.Password)]
    [Display(Name = "New Password")]
    public string NewPassword { get; set; } = "";

    [Required(ErrorMessage = "Confirm the new password.")]
    [DataType(DataType.Password)]
    [Compare(nameof(NewPassword), ErrorMessage = "The two passwords don't match.")]
    [Display(Name = "Confirm New Password")]
    public string ConfirmPassword { get; set; } = "";
}

public class PlatformSettingsViewModel
{
    [Range(0, 5000, ErrorMessage = "Delivery fee must be between ₹0 and ₹5,000.")]
    [Display(Name = "Flat Delivery Fee (₹)")]
    public decimal FlatDeliveryFee { get; set; }

    [Range(0, 1000000, ErrorMessage = "Enter an amount between ₹0 and ₹10,00,000.")]
    [Display(Name = "Free Delivery Above (₹)")]
    public decimal FreeDeliveryAbove { get; set; }

    [Required(ErrorMessage = "Enter a support email.")]
    [EmailAddress(ErrorMessage = "Enter a valid email address.")]
    [StringLength(120)]
    [Display(Name = "Support Email")]
    public string SupportEmail { get; set; } = "";

    [Required(ErrorMessage = "Enter a support phone number.")]
    [StringLength(30)]
    [Display(Name = "Support Phone")]
    public string SupportPhone { get; set; } = "";
}
