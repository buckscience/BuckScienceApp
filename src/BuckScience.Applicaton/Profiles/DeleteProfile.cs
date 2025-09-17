using BuckScience.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Profiles;

public static class DeleteProfile
{
    public sealed record Command(int Id);

    public static async Task HandleAsync(
        Command cmd,
        IAppDbContext db,
        int userId,
        CancellationToken ct = default)
    {
        // Get profile and validate ownership through property
        var profile = await db.Profiles
            .Join(db.Properties, p => p.PropertyId, prop => prop.Id, (p, prop) => new { Profile = p, Property = prop })
            .Where(x => x.Profile.Id == cmd.Id && x.Property.ApplicationUserId == userId)
            .Select(x => x.Profile)
            .FirstOrDefaultAsync(ct);

        if (profile == null)
            throw new UnauthorizedAccessException("Profile not found or access denied.");

        db.Profiles.Remove(profile);
        await db.SaveChangesAsync(ct);
    }
}