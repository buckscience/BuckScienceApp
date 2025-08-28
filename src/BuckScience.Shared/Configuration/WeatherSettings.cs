using System.ComponentModel.DataAnnotations;

namespace BuckScience.Shared.Configuration;

public class WeatherSettings
{
    public const string SectionName = "WeatherSettings";

    [Range(0, 10)]
    public int LocationRoundingPrecision { get; set; } = 2;

    public bool EnableGPSExtraction { get; set; } = true;

    public bool FallbackToCameraLocation { get; set; } = true;
}