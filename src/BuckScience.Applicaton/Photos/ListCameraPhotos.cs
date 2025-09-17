using BuckScience.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Photos;

public static class ListCameraPhotos
{
    public sealed record PhotoListItem(
        int Id,
        string PhotoUrl,
        DateTime DateTaken,
        DateTime DateUploaded
    );

    public static async Task<List<PhotoListItem>> HandleAsync(
        IAppDbContext db,
        int userId,
        int cameraId,
        CancellationToken ct)
    {
        // Verify camera ownership through property using explicit join
        var camera = await db.Cameras
            .Join(db.Properties, c => c.PropertyId, p => p.Id, (c, p) => new { Camera = c, Property = p })
            .Where(x => x.Camera.Id == cameraId && 
                       x.Property.ApplicationUserId == userId)
            .Select(x => x.Camera)
            .FirstOrDefaultAsync(ct);

        if (camera is null)
            throw new KeyNotFoundException("Camera not found or not owned by user.");

        var photos = await db.Photos
            .Where(p => p.CameraId == cameraId)
            .OrderByDescending(p => p.DateTaken)
            .Select(p => new PhotoListItem(
                p.Id,
                p.PhotoUrl,
                p.DateTaken,
                p.DateUploaded
            ))
            .ToListAsync(ct);

        return photos;
    }
}