using BuckScience.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Cameras;

public static class ListPropertyCameras
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
        DateTime CreatedDate
    );

    // List cameras for a single property owned by userId
    public static async Task<IReadOnlyList<Result>> HandleAsync(
        IAppDbContext db,
        int userId,
        int propertyId,
        CancellationToken ct)
    {
        // ownership enforced via property join
        return await db.Cameras
            .AsNoTracking()
            .Where(c => c.PropertyId == propertyId && c.Property.ApplicationUserId == userId)
            .OrderBy(c => c.Name)
            .Select(c => new Result(
                c.Id,
                c.Name,
                c.Brand,
                c.Model,
                c.Latitude,
                c.Longitude,
                c.IsActive,
                c.PhotoCount,
                c.CreatedDate))
            .ToListAsync(ct);
    }
}