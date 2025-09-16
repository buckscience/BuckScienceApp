using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Services;
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
        IList<FileData> Files
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
        // Verify camera ownership through property using explicit join and include placement histories
        var camera = await db.Cameras
            .Include(c => c.PlacementHistories)
            .Join(db.Properties, c => c.PropertyId, p => p.Id, (c, p) => new { Camera = c, Property = p })
            .Where(x => x.Camera.Id == cmd.CameraId && 
                       x.Property.ApplicationUserId == userId)
            .Select(x => x.Camera)
            .FirstOrDefaultAsync(ct);

        if (camera is null)
            throw new KeyNotFoundException("Camera not found or not owned by user.");

        // Get current placement for associating with photos
        var currentPlacement = camera.GetCurrentPlacement();

        var photoIds = new List<int>();
        var photosForWeatherProcessing = new List<(Photo photo, (double Latitude, double Longitude)? location, DateTime dateTaken)>();

        // First pass: Create photos and upload files without weather processing
        foreach (var file in cmd.Files)
        {
            if (file.Length > 0)
            {
                // Extract date taken from EXIF data
                var dateTaken = ExtractDateTakenFromExif(file.Content) ?? DateTime.UtcNow;
                
                // Extract GPS location from EXIF data if available
                var exifLocation = ExtractLocationFromExif(file.Content, weatherSettings.Value);
                
                // Create Photo entity first with placeholder URL to get the ID for metadata
                // Only set placement history ID if it's valid (entities saved to database have ID > 0)
                var placementHistoryId = currentPlacement?.Id > 0 ? (int?)currentPlacement.Id : null;
                var photo = new Photo(cmd.CameraId, "UPLOADING", dateTaken, cameraPlacementHistoryId: placementHistoryId);
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
                
                // Prepare for batch weather processing
                if (exifLocation.HasValue || weatherSettings.Value.FallbackToCameraLocation)
                {
                    var location = exifLocation ?? (camera.Latitude, camera.Longitude);
                    photosForWeatherProcessing.Add((photo, location, dateTaken));
                }

                await db.SaveChangesAsync(ct);
                photoIds.Add(photo.Id);
            }
        }

        // Second pass: Batch process weather data by grouping photos by location and date
        if (photosForWeatherProcessing.Any())
        {
            await ProcessWeatherDataInBatches(photosForWeatherProcessing, weatherService, weatherSettings.Value, logger, ct);
            await db.SaveChangesAsync(ct);
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

    private static async Task ProcessWeatherDataInBatches(
        List<(Photo photo, (double Latitude, double Longitude)? location, DateTime dateTaken)> photosForWeatherProcessing,
        IWeatherService weatherService,
        WeatherSettings settings,
        ILogger logger,
        CancellationToken cancellationToken)
    {
        // Group photos by rounded location and date for batch processing
        var photoGroups = photosForWeatherProcessing
            .Where(p => p.location.HasValue)
            .GroupBy(p => 
            {
                var (roundedLat, roundedLon) = weatherService.RoundCoordinates(
                    p.location!.Value.Latitude, 
                    p.location!.Value.Longitude, 
                    settings.LocationRoundingPrecision);
                var date = DateOnly.FromDateTime(p.dateTaken);
                return new { Latitude = roundedLat, Longitude = roundedLon, Date = date };
            })
            .ToList();

        logger.LogInformation("Processing weather data for {GroupCount} location/date groups from {PhotoCount} photos", 
            photoGroups.Count, photosForWeatherProcessing.Count);

        foreach (var group in photoGroups)
        {
            try
            {
                var groupKey = group.Key;
                var photosInGroup = group.ToList();
                
                logger.LogInformation("Processing weather for {PhotoCount} photos at {Latitude}, {Longitude} on {Date}", 
                    photosInGroup.Count, groupKey.Latitude, groupKey.Longitude, groupKey.Date);

                // Check if any weather data exists for this location/date
                var hasExistingWeather = await weatherService.HasWeatherDataForLocationAndDateAsync(
                    groupKey.Latitude, groupKey.Longitude, groupKey.Date, cancellationToken);

                List<Weather> weatherRecords;
                if (!hasExistingWeather)
                {
                    // Fetch weather data for the entire day (single API call for all photos in this group)
                    weatherRecords = await weatherService.FetchDayWeatherDataAsync(
                        groupKey.Latitude, groupKey.Longitude, groupKey.Date, cancellationToken);
                }
                else
                {
                    // Weather data already exists, just retrieve it
                    weatherRecords = await weatherService.GetWeatherDataForLocationAndDateAsync(
                        groupKey.Latitude, groupKey.Longitude, groupKey.Date, cancellationToken);
                }

                // Assign weather records to photos based on their hour
                foreach (var (photo, _, dateTaken) in photosInGroup)
                {
                    var hour = dateTaken.Hour;
                    var weatherRecord = weatherRecords.FirstOrDefault(w => w.Hour == hour);
                    
                    if (weatherRecord != null)
                    {
                        photo.SetWeather(weatherRecord.Id);
                        logger.LogDebug("Assigned weather ID {WeatherId} to photo {PhotoId} for hour {Hour}", 
                            weatherRecord.Id, photo.Id, hour);
                    }
                    else
                    {
                        logger.LogWarning("No weather data found for photo {PhotoId} at hour {Hour}", photo.Id, hour);
                    }
                }
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Failed to process weather data for location {Latitude}, {Longitude} on {Date}. Photos in this group will not have weather data.", 
                    group.Key.Latitude, group.Key.Longitude, group.Key.Date);
                // Continue processing other groups even if one fails
            }
        }
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