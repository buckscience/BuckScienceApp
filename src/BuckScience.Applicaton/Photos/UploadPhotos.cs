using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using BuckScience.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;
using GpsDirectory = MetadataExtractor.Formats.Exif.GpsDirectory;

namespace BuckScience.Application.Photos;

public static class UploadPhotos
{
    public sealed record FileData(
        string FileName,
        Stream Content,
        long Length
    );

    public sealed record Command(
        int CameraId,
        IList<FileData> Files,
        string? Caption = null
    );

    public static async Task<List<int>> HandleAsync(
        Command cmd,
        IAppDbContext db,
        int userId,
        IBlobStorageService blobStorageService,
        IWeatherService weatherService,
        IOptions<WeatherSettings> weatherSettings,
        ILogger logger,
        CancellationToken ct)
    {
        // Verify camera ownership through property
        var camera = await db.Cameras
            .Include(c => c.Property)
            .FirstOrDefaultAsync(c => 
                c.Id == cmd.CameraId && 
                c.Property.ApplicationUserId == userId, ct);

        if (camera is null)
            throw new KeyNotFoundException("Camera not found or not owned by user.");

        var photoIds = new List<int>();

        foreach (var file in cmd.Files)
        {
            if (file.Length > 0)
            {
                // Extract date taken from EXIF data
                var dateTaken = ExtractDateTakenFromExif(file.Content) ?? DateTime.UtcNow;
                
                // Extract GPS location from EXIF data if available
                var exifLocation = ExtractLocationFromExif(file.Content, weatherSettings.Value);
                
                // Create Photo entity first with placeholder URL to get the ID for metadata
                var photo = new Photo(cmd.CameraId, "UPLOADING", dateTaken);
                db.Photos.Add(photo);
                await db.SaveChangesAsync(ct);
                
                // Upload to Azure Blob Storage with metadata
                var blobUrl = await blobStorageService.UploadPhotoAsync(
                    file.Content, 
                    file.FileName, 
                    userId, 
                    cmd.CameraId, 
                    photo.Id, 
                    ct);
                
                // Update the photo with the blob URL
                photo.SetPhotoUrl(blobUrl);

                // Handle weather lookup and assignment
                if (exifLocation.HasValue || weatherSettings.Value.FallbackToCameraLocation)
                {
                    try
                    {
                        var location = exifLocation ?? (camera.Latitude, camera.Longitude);
                        var weatherId = await FindOrCreateWeatherDataAsync(
                            location.Latitude, 
                            location.Longitude, 
                            dateTaken, 
                            weatherService, 
                            weatherSettings.Value,
                            logger,
                            ct);
                        
                        if (weatherId.HasValue)
                        {
                            photo.SetWeather(weatherId.Value);
                        }
                    }
                    catch (Exception ex)
                    {
                        logger.LogWarning(ex, "Failed to assign weather data to photo {PhotoId}", photo.Id);
                        // Continue processing without weather data
                    }
                }

                await db.SaveChangesAsync(ct);
                
                photoIds.Add(photo.Id);
            }
        }

        return photoIds;
    }
    
    private static DateTime? ExtractDateTakenFromExif(Stream imageStream)
    {
        try
        {
            // Reset stream position
            imageStream.Position = 0;
            
            // Extract metadata from the image
            var directories = ImageMetadataReader.ReadMetadata(imageStream);
            
            // Look for EXIF SubIFD directory which contains DateTime Original
            var exifSubIfdDirectory = directories.OfType<ExifSubIfdDirectory>().FirstOrDefault();
            if (exifSubIfdDirectory != null)
            {
                // Try to get DateTimeOriginal first (when photo was taken)
                if (exifSubIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTimeOriginal, out var dateTimeOriginal))
                {
                    return dateTimeOriginal;
                }
                
                // Fallback to DateTime (when photo was digitized)
                if (exifSubIfdDirectory.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dateTime))
                {
                    return dateTime;
                }
            }
            
            // Look for EXIF IFD0 directory as another fallback
            var exifIfd0Directory = directories.OfType<ExifIfd0Directory>().FirstOrDefault();
            if (exifIfd0Directory != null)
            {
                if (exifIfd0Directory.TryGetDateTime(ExifDirectoryBase.TagDateTime, out var dateTime))
                {
                    return dateTime;
                }
            }
        }
        catch (Exception)
        {
            // If EXIF reading fails, return null to fall back to current time
        }
        finally
        {
            // Reset stream position for subsequent operations
            imageStream.Position = 0;
        }
        
        return null;
    }

    private static (double Latitude, double Longitude)? ExtractLocationFromExif(Stream imageStream, WeatherSettings settings)
    {
        if (!settings.EnableGPSExtraction)
            return null;

        try
        {
            // Reset stream position
            imageStream.Position = 0;

            // Extract metadata from the image
            var directories = ImageMetadataReader.ReadMetadata(imageStream);

            // Look for GPS directory
            var gpsDirectory = directories.OfType<GpsDirectory>().FirstOrDefault();
            if (gpsDirectory != null && gpsDirectory.HasTagName(GpsDirectory.TagLatitude) && gpsDirectory.HasTagName(GpsDirectory.TagLongitude))
            {
                var location = gpsDirectory.GetGeoLocation();
                if (location != null && !location.IsZero)
                {
                    return (location.Latitude, location.Longitude);
                }
            }
        }
        catch (Exception)
        {
            // If GPS reading fails, return null to fall back to camera location if enabled
        }
        finally
        {
            // Reset stream position for subsequent operations
            imageStream.Position = 0;
        }

        return null;
    }

    private static async Task<int?> FindOrCreateWeatherDataAsync(
        double latitude,
        double longitude,
        DateTime photoDateTime,
        IWeatherService weatherService,
        WeatherSettings settings,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Round coordinates for weather lookup
        var (roundedLat, roundedLon) = weatherService.RoundCoordinates(latitude, longitude, settings.LocationRoundingPrecision);
        var date = DateOnly.FromDateTime(photoDateTime);
        var hour = photoDateTime.Hour;

        // Check if weather record already exists
        var existingWeather = await weatherService.FindWeatherRecordAsync(roundedLat, roundedLon, date, hour, cancellationToken);
        if (existingWeather != null)
        {
            return existingWeather.Id;
        }

        // Fetch weather data for the entire day if not exists
        logger.LogInformation("Fetching weather data for {Date} at {Latitude}, {Longitude}", 
            date, roundedLat, roundedLon);

        var dayWeatherData = await weatherService.FetchDayWeatherDataAsync(roundedLat, roundedLon, date, cancellationToken);
        
        // Find the specific hour we need
        var hourlyWeather = dayWeatherData.FirstOrDefault(w => w.Hour == hour);
        return hourlyWeather?.Id;
    }
}