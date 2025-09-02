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

        // Get property IDs for the photos being tagged (through camera relationship)
        var propertyIds = await db.Photos
            .Where(p => cmd.PhotoIds.Contains(p.Id))
            .Join(db.Cameras, p => p.CameraId, c => c.Id, (p, c) => c.PropertyId)
            .Distinct()
            .ToListAsync(ct);

        // Create property tags for any property-tag combinations that don't already exist
        foreach (var propertyId in propertyIds)
        {
            var propertyTagExists = await db.PropertyTags
                .AnyAsync(pt => pt.PropertyId == propertyId && pt.TagId == tag.Id, ct);
            
            if (!propertyTagExists)
            {
                var propertyTag = new PropertyTag(propertyId, tag.Id);
                db.PropertyTags.Add(propertyTag);
            }
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

            // Get property IDs for the photos that had tags removed (through camera relationship)
            var propertyIds = await db.Photos
                .Where(p => cmd.PhotoIds.Contains(p.Id))
                .Join(db.Cameras, p => p.CameraId, c => c.Id, (p, c) => c.PropertyId)
                .Distinct()
                .ToListAsync(ct);

            // Check each property to see if the tag is still in use on that property
            var propertyTagsToRemove = new List<PropertyTag>();
            
            foreach (var propertyId in propertyIds)
            {
                // Check if any photos on this property still use this tag
                var tagStillUsedOnProperty = await db.Photos
                    .Join(db.Cameras, p => p.CameraId, c => c.Id, (p, c) => new { p.Id, c.PropertyId })
                    .Where(pc => pc.PropertyId == propertyId)
                    .Join(db.PhotoTags, pc => pc.Id, pt => pt.PhotoId, (pc, pt) => pt.TagId)
                    .AnyAsync(tagId => tagId == cmd.TagId, ct);

                if (!tagStillUsedOnProperty)
                {
                    // Remove PropertyTag entry for this property-tag combination
                    var propertyTagsForProperty = await db.PropertyTags
                        .Where(pt => pt.PropertyId == propertyId && pt.TagId == cmd.TagId)
                        .ToListAsync(ct);
                    propertyTagsToRemove.AddRange(propertyTagsForProperty);
                }
            }

            if (propertyTagsToRemove.Any())
            {
                db.PropertyTags.RemoveRange(propertyTagsToRemove);
                await db.SaveChangesAsync(ct);
            }

            // Check if the tag is still used anywhere in the system
            var tagStillUsedAnywhere = await db.PhotoTags
                .AnyAsync(pt => pt.TagId == cmd.TagId, ct) ||
                await db.PropertyTags
                .AnyAsync(pt => pt.TagId == cmd.TagId, ct);

            if (!tagStillUsedAnywhere)
            {
                // Check if it's a default tag before removing
                var tag = await db.Tags.FirstOrDefaultAsync(t => t.Id == cmd.TagId, ct);
                if (tag != null && !tag.isDefaultTag)
                {
                    db.Tags.Remove(tag);
                    await db.SaveChangesAsync(ct);
                }
            }
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