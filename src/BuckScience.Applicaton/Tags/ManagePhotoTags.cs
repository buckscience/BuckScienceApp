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

        // Get existing photo tags to avoid duplicates - use individual queries to ensure EF compatibility
        var existingPhotoIds = new HashSet<int>();

        foreach (var photoId in cmd.PhotoIds)
        {
            var exists = await db.PhotoTags
                .AnyAsync(pt => pt.PhotoId == photoId && pt.TagId == tag.Id, ct);
            if (exists)
            {
                existingPhotoIds.Add(photoId);
            }
        }

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

        // Find and remove existing photo tags - use individual queries to ensure EF compatibility  
        var photoTagsToRemove = new List<PhotoTag>();

        foreach (var photoId in cmd.PhotoIds)
        {
            var photoTags = await db.PhotoTags
                .Where(pt => pt.PhotoId == photoId && pt.TagId == cmd.TagId)
                .ToListAsync(ct);
            photoTagsToRemove.AddRange(photoTags);
        }

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
        var tagData = await (
            from pt in db.PropertyTags
            join t in db.Tags on pt.TagId equals t.Id
            where pt.PropertyId == propertyId
            select new
            {
                t.Id,
                t.TagName
            }
        )
        .OrderBy(x => x.TagName) // Sorting is done in SQL
        .ToListAsync(ct);         // Query runs in the database

        // Now construct TagInfo instances in memory
        return tagData.Select(x => new TagInfo(x.Id, x.TagName)).ToList();

    }

    public sealed record TagInfo(int Id, string Name);
}