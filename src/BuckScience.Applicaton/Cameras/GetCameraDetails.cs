using BuckScience.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Cameras;

public static class GetCameraDetails
{
    public sealed record Result(
        int Id,
        string LocationName,
        string Brand,
        string? Model,
        double Latitude,
        double Longitude,
        float DirectionDegrees,
        DateTime? CurrentPlacementStartDate,
        bool IsActive,
        int PhotoCount,
        DateTime CreatedDate,
        int PropertyId,
        string PropertyName,
        double PropertyLatitude,
        double PropertyLongitude
    );

    public static async Task<Result?> HandleAsync(
        IAppDbContext db,
        int userId,
        int cameraId,
        CancellationToken ct)
    {
        // Verify camera ownership through property and get camera details using explicit joins
        var camera = await db.Cameras
            .AsNoTracking()
            .Include(c => c.PlacementHistories)
            .Join(db.Properties, c => c.PropertyId, p => p.Id, (c, p) => new { Camera = c, Property = p })
            .GroupJoin(db.Photos, x => x.Camera.Id, photo => photo.CameraId, (x, photos) => new { x.Camera, x.Property, PhotoCount = photos.Count() })
            .Where(x => x.Camera.Id == cameraId && x.Property.ApplicationUserId == userId)
            .Select(x => new {
                Camera = x.Camera,
                Property = x.Property,
                PhotoCount = x.PhotoCount,
                CurrentPlacement = x.Camera.PlacementHistories
                    .Where(ph => ph.EndDateTime == null)
                    .FirstOrDefault()
            })
            .FirstOrDefaultAsync(ct);

        if (camera == null)
            return null;

        return new Result(
            camera.Camera.Id,
            camera.CurrentPlacement?.LocationName ?? "",
            camera.Camera.Brand,
            camera.Camera.Model,
            camera.CurrentPlacement?.Latitude ?? 0d,
            camera.CurrentPlacement?.Longitude ?? 0d,
            camera.CurrentPlacement?.DirectionDegrees ?? 0f,
            camera.CurrentPlacement?.StartDateTime,
            camera.Camera.IsActive,
            camera.PhotoCount,
            camera.Camera.CreatedDate,
            camera.Camera.PropertyId,
            camera.Property.Name,
            camera.Property.Latitude,
            camera.Property.Longitude);
    }
}