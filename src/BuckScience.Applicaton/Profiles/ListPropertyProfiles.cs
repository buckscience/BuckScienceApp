using BuckScience.Application.Abstractions;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Profiles;

public static class ListPropertyProfiles
{
    public sealed record Result(
        int Id,
        string Name,
        ProfileStatus ProfileStatus,
        string TagName,
        DateTime CreatedDate,
        string? CoverPhotoUrl
    );

    // List profiles for a single property owned by userId
    public static async Task<IReadOnlyList<Result>> HandleAsync(
        IAppDbContext db,
        int userId,
        int propertyId,
        CancellationToken ct)
    {
        // ownership enforced via property join using explicit joins to avoid LINQ translation errors
        return await db.Profiles
            .AsNoTracking()
            .Join(db.Properties, p => p.PropertyId, prop => prop.Id, (p, prop) => new { Profile = p, Property = prop })
            .Where(x => x.Profile.PropertyId == propertyId && x.Property.ApplicationUserId == userId)
            .Join(db.Tags, x => x.Profile.TagId, t => t.Id, (x, t) => new { x.Profile, x.Property, Tag = t })
            .OrderBy(x => x.Profile.Name)
            .Select(x => new Result(
                x.Profile.Id,
                x.Profile.Name,
                x.Profile.ProfileStatus,
                x.Tag.TagName,
                DateTime.UtcNow, // Using UtcNow as placeholder since Profile doesn't have CreatedDate
                x.Profile.CoverPhotoUrl))
            .ToListAsync(ct);
    }
}