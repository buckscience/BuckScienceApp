using BuckScience.Functions.Services;
using BuckScience.Shared.Photos;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace BuckScience.Functions;

public class ProcessPhotoQueue
{
    private readonly WeatherDbContext _context;
    private readonly IWeatherCacheService _weatherService;
    private readonly ILogger<ProcessPhotoQueue> _logger;

    public ProcessPhotoQueue(WeatherDbContext context, IWeatherCacheService weatherService, ILogger<ProcessPhotoQueue> logger)
    {
        _context = context;
        _weatherService = weatherService;
        _logger = logger;
    }

    [Function("ProcessPhotoQueue")]
    public async Task Run([QueueTrigger("photo-ingest", Connection = "AzureWebJobsStorage")] string queueMessage)
    {
        try
        {
            _logger.LogInformation("Processing photo ingest message: {Message}", queueMessage);

            // Deserialize the queue message
            var message = JsonSerializer.Deserialize<PhotoIngestMessage>(queueMessage);
            if (message == null)
            {
                _logger.LogError("Failed to deserialize queue message: {Message}", queueMessage);
                return;
            }

            // Find the photo record
            var photo = await _context.Photos.FindAsync(message.PhotoId);
            if (photo == null)
            {
                _logger.LogError("Photo with ID {PhotoId} not found", message.PhotoId);
                return;
            }

            // Update photo status to 'processing'
            photo.Status = "processing";
            photo.UpdatedAtUtc = DateTime.UtcNow;

            string? weatherJson = null;

            // Try to get weather data if we have location and timestamp
            if (message.TakenAtUtc.HasValue)
            {
                weatherJson = await _weatherService.GetWeatherDataAsync(
                    message.CameraId, 
                    message.TakenAtUtc.Value, 
                    message.Latitude, 
                    message.Longitude);
            }
            else
            {
                _logger.LogInformation("Photo {PhotoId} has no timestamp, skipping weather enrichment", message.PhotoId);
            }

            // Update photo with weather data and mark as ready
            photo.WeatherJson = weatherJson;
            photo.Status = "ready";
            photo.UpdatedAtUtc = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            _logger.LogInformation("Successfully processed photo {PhotoId} for user {UserId}, camera {CameraId}. Weather data: {HasWeather}", 
                message.PhotoId, message.UserId, message.CameraId, weatherJson != null ? "Yes" : "No");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to process photo ingest message: {Message}", queueMessage);

            // Try to update photo status to 'failed' if we can identify the photo
            try
            {
                if (!string.IsNullOrEmpty(queueMessage))
                {
                    var message = JsonSerializer.Deserialize<PhotoIngestMessage>(queueMessage);
                    if (message?.PhotoId > 0)
                    {
                        var photo = await _context.Photos.FindAsync(message.PhotoId);
                        if (photo != null)
                        {
                            photo.Status = "failed";
                            photo.UpdatedAtUtc = DateTime.UtcNow;
                            await _context.SaveChangesAsync();
                        }
                    }
                }
            }
            catch (Exception updateEx)
            {
                _logger.LogError(updateEx, "Failed to update photo status to 'failed' after processing error");
            }

            throw; // Re-throw to trigger retry logic
        }
    }
}