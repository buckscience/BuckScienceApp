using BuckScience.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Cameras;

public static class GetCameraDetails
{
    public sealed record Result(
        int Id,
        string Name,
        string Brand,
        string? Model,
        double Latitude,
        double Longitude,
        bool IsActive,
        int PhotoCount,
        DateTime CreatedDate,
        int PropertyId,
        string PropertyName
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
            .Join(db.Properties, c => c.PropertyId, p => p.Id, (c, p) => new { Camera = c, Property = p })
            .GroupJoin(db.Photos, x => x.Camera.Id, photo => photo.CameraId, (x, photos) => new { x.Camera, x.Property, PhotoCount = photos.Count() })
            .Where(x => x.Camera.Id == cameraId && x.Property.ApplicationUserId == userId)
            .Select(x => new Result(
                x.Camera.Id,
                x.Camera.Name,
                x.Camera.Brand,
                x.Camera.Model,
                x.Camera.Latitude,
                x.Camera.Longitude,
                x.Camera.IsActive,
                x.PhotoCount,
                x.Camera.CreatedDate,
                x.Camera.PropertyId,
                x.Property.Name))
            .FirstOrDefaultAsync(ct);

        return camera;
    }
}