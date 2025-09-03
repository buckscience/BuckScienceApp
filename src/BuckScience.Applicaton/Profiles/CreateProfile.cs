using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Profiles;

public static class CreateProfile
{
    public sealed record Command(
        string Name,
        int PropertyId,
        int TagId,
        ProfileStatus ProfileStatus
    );

    public static async Task<int> HandleAsync(
        Command cmd,
        IAppDbContext db,
        int userId,
        CancellationToken ct = default)
    {
        // Validate property ownership
        var property = await db.Properties
            .Where(p => p.Id == cmd.PropertyId && p.ApplicationUserId == userId)
            .FirstOrDefaultAsync(ct);

        if (property == null)
            throw new UnauthorizedAccessException("Property not found or access denied.");

        // Validate tag exists and belongs to the property
        var tag = await db.Tags
            .Where(t => t.Id == cmd.TagId)
            .FirstOrDefaultAsync(ct);

        if (tag == null)
            throw new ArgumentException("Tag not found.", nameof(cmd.TagId));

        // Create the profile
        var profile = new Profile(cmd.Name, cmd.PropertyId, cmd.TagId, cmd.ProfileStatus);

        // Set cover photo to the most recent photo with this tag
        var coverPhotoUrl = await GetCoverPhotoForTag(cmd.TagId, cmd.PropertyId, db, ct);
        if (coverPhotoUrl != null)
        {
            profile.SetCoverPhoto(coverPhotoUrl);
        }

        db.Profiles.Add(profile);
        await db.SaveChangesAsync(ct);

        return profile.Id;
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