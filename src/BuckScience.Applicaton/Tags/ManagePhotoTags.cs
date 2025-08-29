using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Tags;

public static class ManagePhotoTags
{
    public sealed record AddTagToPhotosCommand(List<int> PhotoIds, string TagName);
    public sealed record RemoveTagFromPhotosCommand(List<int> PhotoIds, int TagId);

    public static async Task AddTagToPhotosAsync(
        AddTagToPhotosCommand cmd,
        IAppDbContext db,
        CancellationToken ct = default)
    {
        if (!cmd.PhotoIds.Any())
            throw new ArgumentException("At least one photo ID is required.", nameof(cmd.PhotoIds));

        // Get or create the tag
        var tag = await GetOrCreateTag.HandleAsync(cmd.TagName, db, ct);

        // Get existing photo tags to avoid duplicates
        var existingPhotoTagPairs = await db.PhotoTags
            .Where(pt => cmd.PhotoIds.Contains(pt.PhotoId) && pt.TagId == tag.Id)
            .Select(pt => new { pt.PhotoId, pt.TagId })
            .ToListAsync(ct);

        var existingPhotoIds = existingPhotoTagPairs.Select(pt => pt.PhotoId).ToHashSet();

        // Create photo tags for photos that don't already have this tag
        var photosToTag = cmd.PhotoIds.Where(photoId => !existingPhotoIds.Contains(photoId));

        foreach (var photoId in photosToTag)
        {
            var photoTag = new PhotoTag(photoId, tag.Id);
            db.PhotoTags.Add(photoTag);
        }

        await db.SaveChangesAsync(ct);
    }

    public static async Task RemoveTagFromPhotosAsync(
        RemoveTagFromPhotosCommand cmd,
        IAppDbContext db,
        CancellationToken ct = default)
    {
        if (!cmd.PhotoIds.Any())
            throw new ArgumentException("At least one photo ID is required.", nameof(cmd.PhotoIds));

        // Find and remove existing photo tags
        var photoTagsToRemove = await db.PhotoTags
            .Where(pt => cmd.PhotoIds.Contains(pt.PhotoId) && pt.TagId == cmd.TagId)
            .ToListAsync(ct);

        if (photoTagsToRemove.Any())
        {
            db.PhotoTags.RemoveRange(photoTagsToRemove);
            await db.SaveChangesAsync(ct);
        }
    }

    public static async Task<List<TagInfo>> GetPhotoTagsAsync(
        int photoId,
        IAppDbContext db,
        CancellationToken ct = default)
    {
        return await db.PhotoTags
            .Where(pt => pt.PhotoId == photoId)
            .Join(db.Tags, pt => pt.TagId, t => t.Id, (pt, t) => new TagInfo(t.Id, t.TagName))
            .ToListAsync(ct);
    }

    public static async Task<List<TagInfo>> GetAvailableTagsForPropertyAsync(
        int propertyId,
        IAppDbContext db,
        CancellationToken ct = default)
    {
        // Get all tags that are associated with the property using explicit join
        return await db.PropertyTags
            .Where(pt => pt.PropertyId == propertyId)
            .Join(db.Tags, pt => pt.TagId, t => t.Id, (pt, t) => new TagInfo(t.Id, t.TagName))
            .OrderBy(t => t.Name)
            .ToListAsync(ct);
    }

    public sealed record TagInfo(int Id, string Name);
}