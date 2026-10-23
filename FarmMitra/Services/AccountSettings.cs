using FarmMitra.Data;
using FarmMitra.Models;
using FarmMitra.ViewModels;
using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace FarmMitra.Services;

/// <summary>Shared profile logic used by the Farmer, Buyer and Admin Profile controllers.</summary>
public static class AccountSettings
{
    public static readonly string[] States =
    {
        "Andhra Pradesh", "Assam", "Bihar", "Chhattisgarh", "Delhi", "Goa", "Gujarat", "Haryana",
        "Himachal Pradesh", "Jharkhand", "Karnataka", "Kerala", "Madhya Pradesh", "Maharashtra",
        "Odisha", "Punjab", "Rajasthan", "Tamil Nadu", "Telangana", "Uttar Pradesh", "Uttarakhand", "West Bengal"
    };

    public static ProfileViewModel ToModel(ApplicationUser u, string role) => Fill(new ProfileViewModel
    {
        FullName = u.FullName,
        Phone = u.PhoneNumber,
        Address = u.Address,
        City = u.City,
        State = u.State,
        FarmLocation = u.FarmLocation,
        CropsGrown = u.CropsGrown,
        FarmSizeAcres = u.FarmSizeAcres,
        ExperienceYears = u.ExperienceYears
    }, u, role);

    /// <summary>Fills the read-only fields (used after a failed post too).</summary>
    public static ProfileViewModel Fill(ProfileViewModel m, ApplicationUser u, string role)
    {
        m.Email = u.Email;
        m.ProfileImagePath = u.ProfileImagePath;
        m.Role = role;
        m.MemberSince = u.CreatedAt;
        m.ApprovalStatus = role == "Farmer" ? u.ApprovalStatus : null;
        return m;
    }

    public static void Validate(ProfileViewModel m, string role, ModelStateDictionary state)
    {
        if (role == "Admin") return;

        if (string.IsNullOrWhiteSpace(m.Phone))
            state.AddModelError(nameof(m.Phone), "Enter a mobile number.");

        if (role == "Farmer")
        {
            if (string.IsNullOrWhiteSpace(m.City)) state.AddModelError(nameof(m.City), "Enter your city.");
            if (string.IsNullOrWhiteSpace(m.FarmLocation)) state.AddModelError(nameof(m.FarmLocation), "Enter your farm location.");
            if (string.IsNullOrWhiteSpace(m.CropsGrown)) state.AddModelError(nameof(m.CropsGrown), "List the crops you grow.");
        }
    }

    /// <summary>Saves a newly chosen photo (deleting the old file) or removes the current one.</summary>
    public static async Task ApplyPhotoAsync(ApplicationUser u, ProfileViewModel m, string webRoot)
    {
        if (m.Photo is { Length: > 0 })
        {
            var old = u.ProfileImagePath;
            u.ProfileImagePath = await ImageUpload.SaveAsync(m.Photo, webRoot, "profiles");
            DeleteFile(old, webRoot);
        }
        else if (m.RemovePhoto && !string.IsNullOrEmpty(u.ProfileImagePath))
        {
            DeleteFile(u.ProfileImagePath, webRoot);
            u.ProfileImagePath = null;
        }
    }

    private static void DeleteFile(string? url, string webRoot)
    {
        // Only ever delete files we saved ourselves, inside uploads/profiles.
        if (string.IsNullOrEmpty(url) || !url.StartsWith("/uploads/profiles/")) return;
        var full = Path.Combine(webRoot, "uploads", "profiles", Path.GetFileName(url));
        try { if (File.Exists(full)) File.Delete(full); } catch { /* not worth failing the request */ }
    }

    public static void Apply(ApplicationUser u, ProfileViewModel m, string role)
    {
        static string? T(string? s) => string.IsNullOrWhiteSpace(s) ? null : s.Trim();

        u.FullName = m.FullName.Trim();
        u.PhoneNumber = T(m.Phone);
        u.Address = T(m.Address);
        u.City = T(m.City);
        u.State = T(m.State);

        // Farm details only ever change for farmers.
        if (role == "Farmer")
        {
            u.FarmLocation = T(m.FarmLocation);
            u.CropsGrown = T(m.CropsGrown);
            u.FarmSizeAcres = m.FarmSizeAcres;
            u.ExperienceYears = m.ExperienceYears;
        }
    }
}
