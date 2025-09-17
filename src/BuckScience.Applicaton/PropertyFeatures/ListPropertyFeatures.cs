using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.PropertyFeatures;

public static class ListPropertyFeatures
{
    public sealed record Result(
        int Id,
        int PropertyId,
        ClassificationType ClassificationType,
        string GeometryWkt,
        string? Name,
        string? Notes,
        float? Weight,
        int? CreatedBy,
        DateTime CreatedAt);

    public static async Task<IReadOnlyList<Result>> HandleAsync(
        IAppDbContext db,
        int userId,
        int propertyId,
        CancellationToken ct)
    {
        // Verify property ownership
        var propertyExists = await db.Properties
            .AnyAsync(p => p.Id == propertyId && p.ApplicationUserId == userId, ct);

        if (!propertyExists)
            return new List<Result>();

        var features = await db.PropertyFeatures
            .AsNoTracking()
            .Where(pf => pf.PropertyId == propertyId)
            .OrderBy(pf => pf.ClassificationType)
            .ThenBy(pf => pf.CreatedAt)
            .Select(pf => new Result(
                pf.Id,
                pf.PropertyId,
                pf.ClassificationType,
                pf.Geometry.AsText(),
                pf.Name,
                pf.Notes,
                pf.Weight,
                pf.CreatedBy,
                pf.CreatedAt))
            .ToListAsync(ct);

        return features;
    }
}