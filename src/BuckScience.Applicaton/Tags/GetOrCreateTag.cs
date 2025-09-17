using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Tags;

public static class GetOrCreateTag
{
    public static async Task<Tag> HandleAsync(
        string tagName,
        IAppDbContext db,
        CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(tagName))
            throw new ArgumentException("Tag name is required.", nameof(tagName));

        var normalizedTagName = tagName.Trim().ToLowerInvariant();

        // Try to find existing tag (case-insensitive)
        var existingTag = await db.Tags
            .FirstOrDefaultAsync(t => t.TagName.ToLower() == normalizedTagName, ct);

        if (existingTag != null)
            return existingTag;

        // Create new tag if it doesn't exist
        var newTag = new Tag(tagName.Trim());
        db.Tags.Add(newTag);
        await db.SaveChangesAsync(ct);

        return newTag;
    }
}