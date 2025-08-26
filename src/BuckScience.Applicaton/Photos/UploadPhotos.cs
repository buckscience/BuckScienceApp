using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using MetadataExtractor;
using MetadataExtractor.Formats.Exif;

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
        string uploadPath,
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
                
                // Create user-specific directory structure
                var userDirectory = Path.Combine(uploadPath, userId.ToString());
                System.IO.Directory.CreateDirectory(userDirectory);
                
                // Create a simple filename. In production, you'd use proper file storage
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var relativePath = $"/uploads/{userId}/{fileName}";
                
                // Save file to user-specific upload path
                var filePath = Path.Combine(userDirectory, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.Content.Position = 0;
                    await file.Content.CopyToAsync(stream, ct);
                }

                // Create Photo entity with EXIF-extracted date
                var photo = new Photo(cmd.CameraId, relativePath, dateTaken);
                db.Photos.Add(photo);
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
}