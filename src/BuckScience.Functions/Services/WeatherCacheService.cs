using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BuckScience.Functions.Services;

/// <summary>
/// Simple EF Context for Functions that only deals with WeatherCache and Photos tables
/// </summary>
public class WeatherDbContext : DbContext
{
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options) { }

    public DbSet<WeatherCacheEntry> WeatherCache { get; set; } = null!;
    public DbSet<PhotoEntry> Photos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // WeatherCache configuration
        modelBuilder.Entity<WeatherCacheEntry>(entity =>
        {
            entity.ToTable("WeatherCache");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CameraId, e.LocalDate }).IsUnique();
            entity.Property(e => e.WeatherJson).HasMaxLength(4000);
        });

        // Photos configuration (simplified for Functions use)
        modelBuilder.Entity<PhotoEntry>(entity =>
        {
            entity.ToTable("Photos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.WeatherJson).HasMaxLength(4000);
        });
    }
}

public class WeatherCacheEntry
{
    public int Id { get; set; }
    public int CameraId { get; set; }
    public DateOnly LocalDate { get; set; }
    public string WeatherJson { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}

public class PhotoEntry
{
    public int Id { get; set; }
    public string UserId { get; set; } = string.Empty;
    public int CameraId { get; set; }
    public string ContentHash { get; set; } = string.Empty;
    public string ThumbBlobName { get; set; } = string.Empty;
    public string DisplayBlobName { get; set; } = string.Empty;
    public DateTime? TakenAtUtc { get; set; }
    public decimal? Latitude { get; set; }
    public decimal? Longitude { get; set; }
    public string? WeatherJson { get; set; }
    public string Status { get; set; } = "processing";
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

/// <summary>
/// Service for caching weather data to avoid duplicate API calls
/// </summary>
public interface IWeatherCacheService
{
    Task<string?> GetWeatherDataAsync(int cameraId, DateTime takenAtUtc, decimal? latitude, decimal? longitude);
}

public class WeatherCacheService : IWeatherCacheService
{
    private readonly WeatherDbContext _context;
    private readonly HttpClient _httpClient;
    private readonly ILogger<WeatherCacheService> _logger;

    public WeatherCacheService(WeatherDbContext context, HttpClient httpClient, ILogger<WeatherCacheService> logger)
    {
        _context = context;
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<string?> GetWeatherDataAsync(int cameraId, DateTime takenAtUtc, decimal? latitude, decimal? longitude)
    {
        try
        {
            // Use UTC date as local date (simplified for now)
            var localDate = DateOnly.FromDateTime(takenAtUtc.Date);

            // Check cache first
            var cached = await _context.WeatherCache
                .FirstOrDefaultAsync(w => w.CameraId == cameraId && w.LocalDate == localDate);

            if (cached != null)
            {
                _logger.LogInformation("Using cached weather data for camera {CameraId} on {LocalDate}", cameraId, localDate);
                return cached.WeatherJson;
            }

            // If no GPS coordinates, we can't fetch weather data
            if (!latitude.HasValue || !longitude.HasValue)
            {
                _logger.LogWarning("No GPS coordinates available for camera {CameraId}, cannot fetch weather data", cameraId);
                return null;
            }

            // Fetch from Open-Meteo ERA5 archive
            var weatherJson = await FetchWeatherDataAsync(latitude.Value, longitude.Value, takenAtUtc);

            if (!string.IsNullOrEmpty(weatherJson))
            {
                // Cache the result
                await CacheWeatherDataAsync(cameraId, localDate, weatherJson);
                _logger.LogInformation("Fetched and cached weather data for camera {CameraId} on {LocalDate}", cameraId, localDate);
            }

            return weatherJson;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to get weather data for camera {CameraId}", cameraId);
            return null;
        }
    }

    private async Task<string?> FetchWeatherDataAsync(decimal latitude, decimal longitude, DateTime takenAtUtc)
    {
        try
        {
            var date = takenAtUtc.ToString("yyyy-MM-dd");
            var url = $"https://archive-api.open-meteo.com/v1/era5" +
                     $"?latitude={latitude}&longitude={longitude}" +
                     $"&start_date={date}&end_date={date}" +
                     $"&hourly=temperature_2m,relative_humidity_2m,precipitation,wind_speed_10m,wind_direction_10m" +
                     $"&timezone=auto";

            _logger.LogInformation("Fetching weather data from: {Url}", url);

            var response = await _httpClient.GetAsync(url);
            if (response.IsSuccessStatusCode)
            {
                var content = await response.Content.ReadAsStringAsync();
                
                // Validate that it's valid JSON
                JsonDocument.Parse(content);
                
                return content;
            }
            else
            {
                _logger.LogWarning("Weather API returned {StatusCode}: {ReasonPhrase}", response.StatusCode, response.ReasonPhrase);
                return null;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to fetch weather data from Open-Meteo API");
            return null;
        }
    }

    private async Task CacheWeatherDataAsync(int cameraId, DateOnly localDate, string weatherJson)
    {
        try
        {
            var entry = new WeatherCacheEntry
            {
                CameraId = cameraId,
                LocalDate = localDate,
                WeatherJson = weatherJson,
                CreatedAtUtc = DateTime.UtcNow
            };

            _context.WeatherCache.Add(entry);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache weather data for camera {CameraId} on {LocalDate}", cameraId, localDate);
            // Don't throw - caching failure shouldn't break the process
        }
    }
}