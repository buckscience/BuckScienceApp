using BuckScience.Domain.Entities;

namespace BuckScience.Application.Abstractions;

public interface IWeatherService
{
    /// <summary>
    /// Fetches 24-hour weather data for a specific location and date from the weather API
    /// </summary>
    /// <param name="latitude">Latitude coordinate</param>
    /// <param name="longitude">Longitude coordinate</param>
    /// <param name="date">Date for weather data</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>List of hourly weather records for the day</returns>
    Task<List<Weather>> FetchDayWeatherDataAsync(double latitude, double longitude, DateOnly date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Finds existing weather record by location, date, and hour
    /// </summary>
    /// <param name="latitude">Rounded latitude</param>
    /// <param name="longitude">Rounded longitude</param>
    /// <param name="date">Date</param>
    /// <param name="hour">Hour (0-23)</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>Weather record if found, null otherwise</returns>
    Task<Weather?> FindWeatherRecordAsync(double latitude, double longitude, DateOnly date, int hour, CancellationToken cancellationToken = default);

    /// <summary>
    /// Rounds coordinates to specified precision for weather lookup
    /// </summary>
    /// <param name="latitude">Latitude</param>
    /// <param name="longitude">Longitude</param>
    /// <param name="precision">Decimal places for rounding</param>
    /// <returns>Rounded coordinates</returns>
    (double RoundedLatitude, double RoundedLongitude) RoundCoordinates(double latitude, double longitude, int precision);
}