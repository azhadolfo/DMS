using System.ComponentModel.DataAnnotations;

namespace Document_Management.Models;

public class AppSetting
{
    [Key]
    public string SettingKey { get; set; }

    public string Value { get; set; }
}