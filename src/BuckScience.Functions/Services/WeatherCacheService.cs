using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Text.Json;
using BuckScience.Domain.Entities;

namespace BuckScience.Functions.Services;

/// <summary>
/// Simple EF Context for Functions that only deals with WeatherCache and Photos tables
/// </summary>
public class WeatherDbContext : DbContext
{
    public WeatherDbContext(DbContextOptions<WeatherDbContext> options) : base(options) { }

    public DbSet<WeatherCache> WeatherCaches { get; set; } = null!;
    public DbSet<Photo> Photos { get; set; } = null!;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // WeatherCache configuration
        modelBuilder.Entity<WeatherCache>(entity =>
        {
            entity.ToTable("WeatherCache");
            entity.HasKey(e => e.Id);
            entity.HasIndex(e => new { e.CameraId, e.LocalDate }).IsUnique();
            entity.Property(e => e.WeatherJson).HasMaxLength(4000);
        });

        // Photos configuration (simplified for Functions use)
        modelBuilder.Entity<Photo>(entity =>
        {
            entity.ToTable("Photos");
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PhotoUrl).HasMaxLength(2048);
            entity.Property(e => e.Status).HasMaxLength(50);
            entity.Property(e => e.UserId).HasMaxLength(450);
            entity.Property(e => e.ContentHash).HasMaxLength(64);
            entity.Property(e => e.ThumbBlobName).HasMaxLength(500);
            entity.Property(e => e.DisplayBlobName).HasMaxLength(500);
            entity.Property(e => e.Latitude).HasPrecision(10, 8);
            entity.Property(e => e.Longitude).HasPrecision(11, 8);
        });
    }
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
            var cached = await _context.WeatherCaches
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
            var entry = new WeatherCache(cameraId, localDate, weatherJson);

            _context.WeatherCaches.Add(entry);
            await _context.SaveChangesAsync();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to cache weather data for camera {CameraId} on {LocalDate}", cameraId, localDate);
            // Don't throw - caching failure shouldn't break the process
        }
    }
}