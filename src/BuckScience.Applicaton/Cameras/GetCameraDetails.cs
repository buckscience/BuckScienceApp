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
        // Verify camera ownership through property and get camera details
        var camera = await db.Cameras
            .AsNoTracking()
            .Include(c => c.Property)
            .Where(c => c.Id == cameraId && c.Property.ApplicationUserId == userId)
            .Select(c => new Result(
                c.Id,
                c.Name,
                c.Brand,
                c.Model,
                c.Latitude,
                c.Longitude,
                c.IsActive,
                c.Photos.Count(),
                c.CreatedDate,
                c.PropertyId,
                c.Property.Name))
            .FirstOrDefaultAsync(ct);

        return camera;
    }
}