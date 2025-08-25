namespace BuckScience.Domain.Entities;

/// <summary>
/// Weather cache entity to store API responses per camera per day
/// </summary>
public class WeatherCache
{
    protected WeatherCache() { } // EF

    public WeatherCache(int cameraId, DateOnly localDate, string weatherJson)
    {
        SetCamera(cameraId);
        SetLocalDate(localDate);
        SetWeatherJson(weatherJson);
        CreatedAtUtc = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int CameraId { get; private set; }
    public DateOnly LocalDate { get; private set; }
    public string WeatherJson { get; private set; } = string.Empty;
    public DateTime CreatedAtUtc { get; private set; }

    // Behavior methods
    public void SetCamera(int cameraId)
    {
        if (cameraId <= 0)
            throw new ArgumentOutOfRangeException(nameof(cameraId));
        CameraId = cameraId;
    }

    public void SetLocalDate(DateOnly localDate)
    {
        LocalDate = localDate;
    }

    public void SetWeatherJson(string weatherJson)
    {
        if (string.IsNullOrWhiteSpace(weatherJson))
            throw new ArgumentException("WeatherJson is required.", nameof(weatherJson));
        WeatherJson = weatherJson.Trim();
    }
}