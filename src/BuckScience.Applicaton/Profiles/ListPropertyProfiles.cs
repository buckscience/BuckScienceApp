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
        // ownership enforced via property join
        return await db.Profiles
            .AsNoTracking()
            .Where(p => p.PropertyId == propertyId && p.Property.ApplicationUserId == userId)
            .OrderBy(p => p.Name)
            .Select(p => new Result(
                p.Id,
                p.Name,
                p.ProfileStatus,
                p.Tag.TagName,
                DateTime.UtcNow)) // Using UtcNow as placeholder since Profile doesn't have CreatedDate
            .ToListAsync(ct);
    }
}