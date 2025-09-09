using BuckScience.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Cameras;

public static class ListPropertyCameras
{
    public sealed record Result(
        int Id,
        string LocationName,
        string Brand,
        string? Model,
        double Latitude,
        double Longitude,
        float DirectionDegrees,
        bool IsActive,
        int PhotoCount,
        DateTime CreatedDate
    );

    // List cameras for a single property owned by userId
    public static async Task<IReadOnlyList<Result>> HandleAsync(
        IAppDbContext db,
        int userId,
        int propertyId,
        CancellationToken ct)
    {
        // ownership enforced via property join using explicit joins to avoid LINQ translation errors
        return await db.Cameras
            .AsNoTracking()
            .Include(c => c.PlacementHistories)
            .Join(db.Properties, c => c.PropertyId, p => p.Id, (c, p) => new { Camera = c, Property = p })
            .Where(x => x.Camera.PropertyId == propertyId && x.Property.ApplicationUserId == userId)
            .GroupJoin(db.Photos, x => x.Camera.Id, photo => photo.CameraId, (x, photos) => new { x.Camera, x.Property, PhotoCount = photos.Count() })
            .OrderBy(x => x.Camera.PlacementHistories.Where(ph => ph.EndDateTime == null).FirstOrDefault().LocationName)
            .Select(x => new {
                Camera = x.Camera,
                PhotoCount = x.PhotoCount,
                CurrentPlacement = x.Camera.PlacementHistories
                    .Where(ph => ph.EndDateTime == null)
                    .FirstOrDefault()
            })
            .Select(x => new Result(
                x.Camera.Id,
                x.CurrentPlacement != null ? x.CurrentPlacement.LocationName : "",
                x.Camera.Brand,
                x.Camera.Model,
                x.CurrentPlacement != null ? x.CurrentPlacement.Latitude : 0d,
                x.CurrentPlacement != null ? x.CurrentPlacement.Longitude : 0d,
                x.CurrentPlacement != null ? x.CurrentPlacement.DirectionDegrees : 0f,
                x.Camera.IsActive,
                x.PhotoCount,
                x.Camera.CreatedDate))
            .ToListAsync(ct);
    }
}