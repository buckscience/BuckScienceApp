using BuckScience.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Cameras;

public static class GetCameraPlacementHistory
{
    public sealed record PlacementHistoryItem(
        int Id,
        double Latitude,
        double Longitude,
        float DirectionDegrees,
        DateTime StartDateTime,
        DateTime? EndDateTime,
        bool IsCurrentPlacement,
        TimeSpan? Duration
    );

    public static async Task<IReadOnlyList<PlacementHistoryItem>> HandleAsync(
        IAppDbContext db,
        int userId,
        int cameraId,
        CancellationToken ct)
    {
        // Verify camera ownership through property and get placement history
        var placementHistory = await db.Cameras
            .AsNoTracking()
            .Include(c => c.PlacementHistories)
            .Join(db.Properties, c => c.PropertyId, p => p.Id, (c, p) => new { Camera = c, Property = p })
            .Where(x => x.Camera.Id == cameraId && x.Property.ApplicationUserId == userId)
            .SelectMany(x => x.Camera.PlacementHistories)
            .OrderByDescending(ph => ph.StartDateTime) // Most recent first
            .Select(ph => new PlacementHistoryItem(
                ph.Id,
                ph.Latitude,
                ph.Longitude,
                ph.DirectionDegrees,
                ph.StartDateTime,
                ph.EndDateTime,
                ph.EndDateTime == null, // IsCurrentPlacement
                ph.EndDateTime != null ? ph.EndDateTime.Value - ph.StartDateTime : (DateTime.UtcNow - ph.StartDateTime) // Duration
            ))
            .ToListAsync(ct);

        return placementHistory;
    }
}