using BuckScience.Application.Abstractions;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Profiles;

public static class GetProfile
{
    public sealed record Result(
        int Id,
        string Name,
        ProfileStatus ProfileStatus,
        int PropertyId,
        string PropertyName,
        int TagId,
        string TagName,
        string? CoverPhotoUrl
    );

    public static async Task<Result?> HandleAsync(
        int profileId,
        IAppDbContext db,
        int userId,
        CancellationToken ct = default)
    {
        // Get profile with related data and validate ownership through property
        var result = await db.Profiles
            .Join(db.Properties, p => p.PropertyId, prop => prop.Id, (p, prop) => new { Profile = p, Property = prop })
            .Where(x => x.Profile.Id == profileId && x.Property.ApplicationUserId == userId)
            .Join(db.Tags, x => x.Profile.TagId, t => t.Id, (x, t) => new { x.Profile, x.Property, Tag = t })
            .Select(x => new Result(
                x.Profile.Id,
                x.Profile.Name,
                x.Profile.ProfileStatus,
                x.Profile.PropertyId,
                x.Property.Name,
                x.Profile.TagId,
                x.Tag.TagName,
                x.Profile.CoverPhotoUrl
            ))
            .FirstOrDefaultAsync(ct);

        return result;
    }
}