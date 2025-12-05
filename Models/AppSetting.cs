using System.ComponentModel.DataAnnotations;

namespace Document_Management.Models;

public class AppSetting
{
    [Key]
    public string SettingKey { get; set; } = string.Empty;

    public string Value { get; set; } = string.Empty;
}