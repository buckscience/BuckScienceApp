using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using BuckScience.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System.Globalization;
using System.Text.Json;

namespace BuckScience.Infrastructure.Services;

public class WeatherService : IWeatherService
{
    private readonly IAppDbContext _dbContext;
    private readonly HttpClient _httpClient;
    private readonly WeatherApiSettings _apiSettings;
    private readonly WeatherSettings _weatherSettings;
    private readonly ILogger<WeatherService> _logger;

    public WeatherService(
        IAppDbContext dbContext,
        HttpClient httpClient,
        IOptions<WeatherApiSettings> apiSettings,
        IOptions<WeatherSettings> weatherSettings,
        ILogger<WeatherService> logger)
    {
        _dbContext = dbContext;
        _httpClient = httpClient;
        _apiSettings = apiSettings.Value;
        _weatherSettings = weatherSettings.Value;
        _logger = logger;
    }

    public async Task<List<Weather>> FetchDayWeatherDataAsync(double latitude, double longitude, DateOnly date, CancellationToken cancellationToken = default)
    {
        var roundedCoords = RoundCoordinates(latitude, longitude, _weatherSettings.LocationRoundingPrecision);
        var dateString = date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
        
        var url = $"{_apiSettings.BaseUrl.TrimEnd('/')}/{roundedCoords.RoundedLatitude},{roundedCoords.RoundedLongitude}/{dateString}?key={_apiSettings.APIKey}&include=hours&unitGroup=metric";

        _logger.LogInformation("Fetching weather data from: {Url}", url.Replace(_apiSettings.APIKey, "***"));

        try
        {
            var response = await _httpClient.GetAsync(url, cancellationToken);
            response.EnsureSuccessStatusCode();

            var content = await response.Content.ReadAsStringAsync(cancellationToken);
            var weatherData = JsonSerializer.Deserialize<VisualCrossingResponse>(content, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });

            if (weatherData?.Days == null || !weatherData.Days.Any())
            {
                _logger.LogWarning("No weather data returned for {Latitude}, {Longitude} on {Date}", 
                    roundedCoords.RoundedLatitude, roundedCoords.RoundedLongitude, date);
                return new List<Weather>();
            }

            var dayData = weatherData.Days.First();
            var weatherRecords = new List<Weather>();

            foreach (var hour in dayData.Hours ?? new List<HourData>())
            {
                var hourDateTime = DateOnly.Parse(dayData.Datetime).ToDateTime(TimeOnly.Parse(hour.Datetime));
                var hourNumber = hourDateTime.Hour;

                var weather = new Weather(
                    roundedCoords.RoundedLatitude,
                    roundedCoords.RoundedLongitude,
                    date,
                    hourNumber,
                    hourDateTime,
                    hour.DatetimeEpoch,
                    hour.Temp,
                    hour.Windspeed,
                    hour.Winddir,
                    hour.WinddirText,
                    hour.Visibility,
                    hour.Pressure,
                    hour.PressureTrend,
                    hour.Humidity,
                    hour.Conditions,
                    hour.Icon,
                    dayData.SunriseEpoch,
                    dayData.SunsetEpoch,
                    hour.Cloudcover,
                    dayData.Moonphase,
                    dayData.MoonphaseText
                );

                weatherRecords.Add(weather);
            }

            // Save all weather records to database
            _dbContext.Weathers.AddRange(weatherRecords);
            await _dbContext.SaveChangesAsync(cancellationToken);

            _logger.LogInformation("Successfully fetched and saved {Count} weather records for {Date} at {Latitude}, {Longitude}", 
                weatherRecords.Count, date, roundedCoords.RoundedLatitude, roundedCoords.RoundedLongitude);

            return weatherRecords;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch weather data for {Latitude}, {Longitude} on {Date}", 
                roundedCoords.RoundedLatitude, roundedCoords.RoundedLongitude, date);
            throw;
        }
    }

    public async Task<Weather?> FindWeatherRecordAsync(double latitude, double longitude, DateOnly date, int hour, CancellationToken cancellationToken = default)
    {
        return await _dbContext.Weathers
            .FirstOrDefaultAsync(w => 
                w.Latitude == latitude && 
                w.Longitude == longitude && 
                w.Date == date && 
                w.Hour == hour, 
                cancellationToken);
    }

    public (double RoundedLatitude, double RoundedLongitude) RoundCoordinates(double latitude, double longitude, int precision)
    {
        var factor = Math.Pow(10, precision);
        return (
            Math.Round(latitude * factor) / factor,
            Math.Round(longitude * factor) / factor
        );
    }

    // DTOs for VisualCrossing API response
    private class VisualCrossingResponse
    {
        public List<DayData>? Days { get; set; }
    }

    private class DayData
    {
        public string Datetime { get; set; } = string.Empty;
        public int SunriseEpoch { get; set; }
        public int SunsetEpoch { get; set; }
        public double Moonphase { get; set; }
        public string? MoonphaseText { get; set; }
        public List<HourData>? Hours { get; set; }
    }

    private class HourData
    {
        public string Datetime { get; set; } = string.Empty;
        public int DatetimeEpoch { get; set; }
        public double Temp { get; set; }
        public double Windspeed { get; set; }
        public double Winddir { get; set; }
        public string? WinddirText { get; set; }
        public double Visibility { get; set; }
        public double Pressure { get; set; }
        public string? PressureTrend { get; set; }
        public double Humidity { get; set; }
        public string? Conditions { get; set; }
        public string? Icon { get; set; }
        public double Cloudcover { get; set; }
    }
}