using BuckScience.Application.Abstractions;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Profiles;

public static class UpdateProfile
{
    public sealed record Command(
        int Id,
        string Name,
        ProfileStatus ProfileStatus,
        string? CoverPhotoUrl = null
    );

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

        // Update profile properties
        profile.Rename(cmd.Name);
        profile.SetStatus(cmd.ProfileStatus);

        // Update cover photo if specified, otherwise keep current or find new one
        if (cmd.CoverPhotoUrl != null)
        {
            profile.SetCoverPhoto(cmd.CoverPhotoUrl);
        }
        else if (string.IsNullOrEmpty(profile.CoverPhotoUrl))
        {
            // If no cover photo is set, try to find one
            var coverPhotoUrl = await GetCoverPhotoForTag(profile.TagId, profile.PropertyId, db, ct);
            if (coverPhotoUrl != null)
            {
                profile.SetCoverPhoto(coverPhotoUrl);
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static async Task<string?> GetCoverPhotoForTag(
        int tagId,
        int propertyId,
        IAppDbContext db,
        CancellationToken ct)
    {
        // Get the most recent photo with this tag from cameras on this property
        var photoUrl = await db.PhotoTags
            .Join(db.Photos, pt => pt.PhotoId, p => p.Id, (pt, p) => new { pt, p })
            .Join(db.Cameras, x => x.p.CameraId, c => c.Id, (x, c) => new { x.pt, x.p, c })
            .Where(x => x.pt.TagId == tagId && x.c.PropertyId == propertyId)
            .OrderByDescending(x => x.p.DateTaken)
            .Select(x => x.p.PhotoUrl)
            .FirstOrDefaultAsync(ct);

        return photoUrl;
    }
}