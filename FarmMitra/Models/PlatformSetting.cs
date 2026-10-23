using System.ComponentModel.DataAnnotations;

namespace FarmMitra.Models;

/// <summary>One admin-editable platform setting stored as a key/value pair.</summary>
public class PlatformSetting
{
    [Key, MaxLength(100)]
    public string Key { get; set; } = "";

    [MaxLength(500)]
    public string Value { get; set; } = "";
}
