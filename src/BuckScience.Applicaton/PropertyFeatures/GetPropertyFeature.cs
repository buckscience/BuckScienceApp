using BuckScience.Application.Abstractions;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.PropertyFeatures;

public static class GetPropertyFeature
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

    public static async Task<Result?> HandleAsync(
        int featureId,
        IAppDbContext db,
        int userId,
        CancellationToken ct)
    {
        var feature = await db.PropertyFeatures
            .AsNoTracking()
            .Include(pf => pf.Property)
            .Where(pf => pf.Id == featureId && pf.Property.ApplicationUserId == userId)
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
            .FirstOrDefaultAsync(ct);

        return feature;
    }
}