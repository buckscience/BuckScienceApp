using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;

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
                // Create a simple filename. In production, you'd use proper file storage
                var fileName = $"{Guid.NewGuid()}_{file.FileName}";
                var relativePath = $"/uploads/photos/{fileName}";
                
                // Create upload directory if it doesn't exist
                Directory.CreateDirectory(uploadPath);
                
                // Save file to upload path
                var filePath = Path.Combine(uploadPath, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                {
                    file.Content.Position = 0;
                    await file.Content.CopyToAsync(stream, ct);
                }

                // Create Photo entity
                var photo = new Photo(cmd.CameraId, relativePath, DateTime.UtcNow);
                db.Photos.Add(photo);
                await db.SaveChangesAsync(ct);
                photoIds.Add(photo.Id);
            }
        }

        return photoIds;
    }
}