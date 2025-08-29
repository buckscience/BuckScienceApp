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
        DateTime CreatedDate
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
            .Where(p => p.PropertyId == propertyId && p.Property.ApplicationUserId == userId)
            .Join(db.Tags, p => p.TagId, t => t.Id, (p, t) => new { p, t })
            .OrderBy(x => x.p.Name)
            .Select(x => new Result(
                x.p.Id,
                x.p.Name,
                x.p.ProfileStatus,
                x.t.TagName,
                DateTime.UtcNow)) // Using UtcNow as placeholder since Profile doesn't have CreatedDate
            .ToListAsync(ct);
    }
}