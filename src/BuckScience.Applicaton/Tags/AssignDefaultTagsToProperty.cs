using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Tags;

public static class AssignDefaultTagsToProperty
{
    public static async Task HandleAsync(
        int propertyId,
        IAppDbContext db,
        CancellationToken ct = default)
    {
        if (propertyId <= 0)
            throw new ArgumentOutOfRangeException(nameof(propertyId));

        // Get all default tags
        var defaultTags = await db.Tags
            .Where(t => t.isDefaultTag)
            .ToListAsync(ct);

        // Get existing property tags to avoid duplicates
        var existingPropertyTagIds = await db.PropertyTags
            .Where(pt => pt.PropertyId == propertyId)
            .Select(pt => pt.TagId)
            .ToListAsync(ct);

        // Create property tags for default tags that aren't already assigned
        var tagsToAssign = defaultTags
            .Where(tag => !existingPropertyTagIds.Contains(tag.Id))
            .ToList();

        foreach (var tag in tagsToAssign)
        {
            var propertyTag = new PropertyTag(propertyId, tag.Id);
            db.PropertyTags.Add(propertyTag);
        }

        if (tagsToAssign.Any())
        {
            await db.SaveChangesAsync(ct);
        }
    }
}