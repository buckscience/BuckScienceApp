using System.Collections.Generic;
using System.Linq;

namespace BuckScience.Shared.Helpers;

public static class WeatherHelpers
{
    /// <summary>
    /// Standard compass wind directions in logical order
    /// </summary>
    public static readonly List<string> StandardWindDirections = new()
    {
        "N",   // North
        "NNE", // North-Northeast  
        "NE",  // Northeast
        "ENE", // East-Northeast
        "E",   // East
        "ESE", // East-Southeast
        "SE",  // Southeast
        "SSE", // South-Southeast
        "S",   // South
        "SSW", // South-Southwest
        "SW",  // Southwest
        "WSW", // West-Southwest
        "W",   // West
        "WNW", // West-Northwest
        "NW",  // Northwest
        "NNW"  // North-Northwest
    };

    /// <summary>
    /// Gets wind direction options with availability status
    /// </summary>
    /// <param name="availableDirections">Wind directions that actually exist in the data</param>
    /// <returns>List of wind direction options with availability</returns>
    public static List<WindDirectionOption> GetWindDirectionOptions(IEnumerable<string> availableDirections)
    {
        var availableSet = new HashSet<string>(availableDirections ?? Enumerable.Empty<string>(), 
            StringComparer.OrdinalIgnoreCase);

        return StandardWindDirections.Select(direction => new WindDirectionOption
        {
            Value = direction,
            DisplayName = direction,
            IsAvailable = availableSet.Contains(direction)
        }).ToList();
    }

    /// <summary>
    /// Converts numeric moon phase value (0.0-1.0) to descriptive text
    /// </summary>
    /// <param name="moonPhase">Moon phase value between 0.0 and 1.0</param>
    /// <returns>Descriptive moon phase text</returns>
    public static string ConvertMoonPhaseToText(double moonPhase)
    {
        // Normalize to 0-1 range
        var phase = Math.Max(0.0, Math.Min(1.0, moonPhase));
        
        return phase switch
        {
            >= 0.0 and < 0.05 => "New Moon",
            >= 0.05 and < 0.2 => "Waxing Crescent",
            >= 0.2 and < 0.3 => "First Quarter",
            >= 0.3 and < 0.45 => "Waxing Gibbous",
            >= 0.45 and < 0.55 => "Full Moon",
            >= 0.55 and < 0.7 => "Waning Gibbous",
            >= 0.7 and < 0.8 => "Last Quarter",
            >= 0.8 and < 0.95 => "Waning Crescent",
            _ => "New Moon"
        };
    }

    /// <summary>
    /// Gets the numeric moon phase value from descriptive text
    /// </summary>
    /// <param name="moonPhaseText">Descriptive moon phase text</param>
    /// <returns>Numeric moon phase value (0.0-1.0) or null if not recognized</returns>
    public static double? ConvertMoonPhaseTextToNumeric(string moonPhaseText)
    {
        if (string.IsNullOrWhiteSpace(moonPhaseText))
            return null;

        return moonPhaseText.Trim().ToLowerInvariant() switch
        {
            "new moon" => 0.0,
            "waxing crescent" => 0.125,
            "first quarter" => 0.25,
            "waxing gibbous" => 0.375,
            "full moon" => 0.5,
            "waning gibbous" => 0.625,
            "last quarter" => 0.75,
            "waning crescent" => 0.875,
            _ => null
        };
    }

    /// <summary>
    /// Converts numeric wind direction in degrees to compass direction text
    /// </summary>
    /// <param name="windDirection">Wind direction in degrees (0-360)</param>
    /// <returns>Compass direction text (N, NNE, NE, etc.)</returns>
    public static string ConvertWindDirectionToText(double windDirection)
    {
        // Normalize to 0-360 range
        var direction = windDirection % 360;
        if (direction < 0) direction += 360;

        // Add 11.25 degrees to shift boundaries and then divide by 22.5
        // This way 0-11.24 and 348.75-360 map to N (index 0)
        // 11.25-33.74 maps to NNE (index 1), etc.
        var index = (int)((direction + 11.25) / 22.5) % 16;
        
        return StandardWindDirections[index];
    }

    /// <summary>
    /// Gets the numeric wind direction value from compass direction text
    /// </summary>
    /// <param name="windDirectionText">Compass direction text (N, NNE, NE, etc.)</param>
    /// <returns>Numeric wind direction in degrees (0-360) or null if not recognized</returns>
    public static double? ConvertWindDirectionTextToNumeric(string windDirectionText)
    {
        if (string.IsNullOrWhiteSpace(windDirectionText))
            return null;

        var index = StandardWindDirections.FindIndex(d => 
            string.Equals(d, windDirectionText.Trim(), StringComparison.OrdinalIgnoreCase));
            
        if (index == -1)
            return null;

        // Each direction is 22.5 degrees apart
        return index * 22.5;
    }
}

public class WindDirectionOption
{
    public string Value { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public bool IsAvailable { get; set; }
}