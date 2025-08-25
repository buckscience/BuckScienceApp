using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Domain.Entities;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Photos;

public static class UploadPhoto
{
    public sealed record Command(
        int CameraId,
        IFormFile File,
        string? Caption,
        DateTime? DateTaken = null
    );

    public static async Task<int> HandleAsync(
        Command cmd,
        IAppDbContext db,
        IFileStorageService fileStorage,
        IPhotoProcessingService photoProcessor,
        ICurrentUserService currentUser,
        CancellationToken ct = default)
    {
        if (currentUser.Id is null)
            throw new UnauthorizedAccessException("User must be authenticated");

        // Validate file
        if (cmd.File is null || cmd.File.Length == 0)
            throw new ArgumentException("File is required", nameof(cmd.File));

        // Validate file type
        var allowedTypes = new[] { "image/jpeg", "image/jpg", "image/png", "image/tiff" };
        if (!allowedTypes.Contains(cmd.File.ContentType?.ToLowerInvariant()))
            throw new ArgumentException($"Unsupported file type: {cmd.File.ContentType}. Allowed types: JPEG, PNG, TIFF");

        // Validate file size (max 50MB for trail camera photos)
        const long maxFileSizeBytes = 50 * 1024 * 1024;
        if (cmd.File.Length > maxFileSizeBytes)
            throw new ArgumentException($"File size too large: {cmd.File.Length} bytes. Maximum allowed: {maxFileSizeBytes} bytes");

        // Verify camera ownership
        var camera = await db.Cameras
            .Include(c => c.Property)
            .FirstOrDefaultAsync(c => 
                c.Id == cmd.CameraId && 
                c.Property.ApplicationUserId == currentUser.Id.Value, ct);

        if (camera is null)
            throw new KeyNotFoundException("Camera not found or not owned by user");

        // Step 1: Store original file temporarily
        var tempFilePath = await fileStorage.StoreTempFileAsync(cmd.File, ct);

        try
        {
            // Step 2: Process the photo (creates display and thumbnail WebP versions)
            var baseFileName = $"{DateTime.UtcNow:yyyyMMdd_HHmmss}_{Guid.NewGuid():N}";
            var processingResult = await photoProcessor.ProcessPhotoAsync(tempFilePath, baseFileName, ct);

            // Step 3: Create Photo entity with processed image URLs
            var dateTaken = cmd.DateTaken ?? ExtractDateTakenFromFile(cmd.File) ?? DateTime.UtcNow;
            
            var photo = new Photo(
                cameraId: cmd.CameraId,
                displayUrl: processingResult.DisplayImageUrl,
                thumbnailUrl: processingResult.ThumbnailImageUrl,
                dateTaken: dateTaken,
                displaySizeBytes: processingResult.DisplayFileSizeBytes,
                thumbnailSizeBytes: processingResult.ThumbnailFileSizeBytes,
                dateUploaded: DateTime.UtcNow
            );

            // Step 4: Save to database
            db.Photos.Add(photo);
            await db.SaveChangesAsync(ct);

            return photo.Id;
        }
        catch
        {
            // Cleanup temp file if processing fails
            await fileStorage.DeleteTempFileAsync(tempFilePath, ct);
            throw;
        }
        // Note: temp file is automatically deleted by PhotoProcessingService after successful processing
    }

    private static DateTime? ExtractDateTakenFromFile(IFormFile file)
    {
        // For now, return null - could implement EXIF reading later
        // This would require additional libraries like MetadataExtractor
        return null;
    }
}