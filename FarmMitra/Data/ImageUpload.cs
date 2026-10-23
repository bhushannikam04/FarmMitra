using Microsoft.AspNetCore.Http;

namespace FarmMitra.Data;

public static class ImageUpload
{
    public const long MaxBytes = 3 * 1024 * 1024;
    private static readonly string[] Allowed = { ".jpg", ".jpeg", ".png", ".webp" };

    // Returns an error message, or null when the file is a safe image.
    public static string? Validate(IFormFile? file)
    {
        if (file == null || file.Length == 0) return null;

        var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
        if (!Allowed.Contains(ext)) return "Please upload a JPG, PNG or WebP image.";
        if (file.Length > MaxBytes) return "The image must be 3 MB or smaller.";

        var header = new byte[12];
        using (var stream = file.OpenReadStream())
            stream.ReadAtLeast(header, header.Length, throwOnEndOfStream: false);

        var jpeg = header[0] == 0xFF && header[1] == 0xD8 && header[2] == 0xFF;
        var png = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
        var webp = header[0] == 0x52 && header[1] == 0x49 && header[2] == 0x46 && header[3] == 0x46
                && header[8] == 0x57 && header[9] == 0x45 && header[10] == 0x42 && header[11] == 0x50;

        return jpeg || png || webp ? null : "That file doesn't look like a valid image.";
    }

    // Saves under wwwroot/uploads/{folder}/ with a random name and returns the public URL.
    public static async Task<string> SaveAsync(IFormFile file, string webRootPath, string folder)
    {
        var dir = Path.Combine(webRootPath, "uploads", folder);
        Directory.CreateDirectory(dir);

        var name = Guid.NewGuid().ToString("N") + Path.GetExtension(file.FileName).ToLowerInvariant();
        await using var stream = new FileStream(Path.Combine(dir, name), FileMode.Create);
        await file.CopyToAsync(stream);

        return $"/uploads/{folder}/{name}";
    }
}
