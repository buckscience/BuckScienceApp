using System.Collections.Generic;
using System.Linq;
using BuckScience.Shared.Helpers;
using WindDirectionOption = BuckScience.Shared.Helpers.WindDirectionOption;

namespace BuckScience.Web.Helpers;

public static class WeatherHelpers
{
    /// <summary>
    /// Standard compass wind directions in logical order
    /// </summary>
    public static readonly List<string> StandardWindDirections = BuckScience.Shared.Helpers.WeatherHelpers.StandardWindDirections;

    /// <summary>
    /// Gets wind direction options with availability status
    /// </summary>
    /// <param name="availableDirections">Wind directions that actually exist in the data</param>
    /// <returns>List of wind direction options with availability</returns>
    public static List<WindDirectionOption> GetWindDirectionOptions(IEnumerable<string> availableDirections)
    {
        return BuckScience.Shared.Helpers.WeatherHelpers.GetWindDirectionOptions(availableDirections);
    }

    /// <summary>
    /// Converts numeric moon phase value (0.0-1.0) to descriptive text
    /// </summary>
    /// <param name="moonPhase">Moon phase value between 0.0 and 1.0</param>
    /// <returns>Descriptive moon phase text</returns>
    public static string ConvertMoonPhaseToText(double moonPhase)
    {
        return BuckScience.Shared.Helpers.WeatherHelpers.ConvertMoonPhaseToText(moonPhase);
    }

    /// <summary>
    /// Gets the numeric moon phase value from descriptive text
    /// </summary>
    /// <param name="moonPhaseText">Descriptive moon phase text</param>
    /// <returns>Numeric moon phase value (0.0-1.0) or null if not recognized</returns>
    public static double? ConvertMoonPhaseTextToNumeric(string moonPhaseText)
    {
        return BuckScience.Shared.Helpers.WeatherHelpers.ConvertMoonPhaseTextToNumeric(moonPhaseText);
    }

    /// <summary>
    /// Converts numeric wind direction in degrees to compass direction text
    /// </summary>
    /// <param name="windDirection">Wind direction in degrees (0-360)</param>
    /// <returns>Compass direction text (N, NNE, NE, etc.)</returns>
    public static string ConvertWindDirectionToText(double windDirection)
    {
        return BuckScience.Shared.Helpers.WeatherHelpers.ConvertWindDirectionToText(windDirection);
    }

    /// <summary>
    /// Gets the numeric wind direction value from compass direction text
    /// </summary>
    /// <param name="windDirectionText">Compass direction text (N, NNE, NE, etc.)</param>
    /// <returns>Numeric wind direction in degrees (0-360) or null if not recognized</returns>
    public static double? ConvertWindDirectionTextToNumeric(string windDirectionText)
    {
        return BuckScience.Shared.Helpers.WeatherHelpers.ConvertWindDirectionTextToNumeric(windDirectionText);
    }
}