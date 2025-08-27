using BuckScience.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Photos;

public static class ListPropertyPhotos
{
    public sealed record PhotoListItem(
        int Id,
        string PhotoUrl,
        DateTime DateTaken,
        DateTime DateUploaded,
        int CameraId,
        string CameraName
    );

    public enum SortBy
    {
        DateTakenDesc,
        DateTakenAsc,
        DateUploadedDesc,
        DateUploadedAsc
    }

    public static async Task<List<PhotoListItem>> HandleAsync(
        IAppDbContext db,
        int userId,
        int propertyId,
        SortBy sortBy = SortBy.DateTakenDesc,
        CancellationToken ct = default)
    {
        // Verify property ownership
        var property = await db.Properties
            .FirstOrDefaultAsync(p => 
                p.Id == propertyId && 
                p.ApplicationUserId == userId, ct);

        if (property is null)
            throw new KeyNotFoundException("Property not found or not owned by user.");

        // Get all photos from all cameras on this property
        var query = db.Photos
            .Include(p => p.Camera)
            .Where(p => p.Camera.PropertyId == propertyId)
            .Where(p => p.Camera.Property.ApplicationUserId == userId);

        // Apply sorting
        query = sortBy switch
        {
            SortBy.DateTakenAsc => query.OrderBy(p => p.DateTaken),
            SortBy.DateTakenDesc => query.OrderByDescending(p => p.DateTaken),
            SortBy.DateUploadedAsc => query.OrderBy(p => p.DateUploaded),
            SortBy.DateUploadedDesc => query.OrderByDescending(p => p.DateUploaded),
            _ => query.OrderByDescending(p => p.DateTaken)
        };

        var photos = await query
            .Select(p => new PhotoListItem(
                p.Id,
                p.PhotoUrl,
                p.DateTaken,
                p.DateUploaded,
                p.CameraId,
                p.Camera.Name
            ))
            .ToListAsync(ct);

        return photos;
    }
}