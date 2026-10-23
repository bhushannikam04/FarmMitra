using Microsoft.AspNetCore.Identity;

namespace FarmMitra.Models;

public class ApplicationUser : IdentityUser
{
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public string ApprovalStatus { get; set; } = "Approved";   // Pending / Approved / Rejected
    public string FullName { get; set; } = "";
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? State { get; set; }
    public bool IsApproved { get; set; } = true;   // set false for farmers at registration

    // Farmer-only fields
    public string? FarmLocation { get; set; }
    public decimal? FarmSizeAcres { get; set; }
    public string? CropsGrown { get; set; }
    public int? ExperienceYears { get; set; }
    public string? ProfileImagePath { get; set; }
}