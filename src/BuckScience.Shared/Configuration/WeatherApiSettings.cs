using System.ComponentModel.DataAnnotations;

namespace BuckScience.Shared.Configuration;

public class WeatherApiSettings
{
    public const string SectionName = "WeatherAPISettings";

    [Required]
    public string BaseUrl { get; set; } = string.Empty;

    [Required]
    public string APIKey { get; set; } = string.Empty;
}