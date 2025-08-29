using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
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
        PhotoFilters? filters = null,
        CancellationToken ct = default)
    {
        // Verify property ownership
        var property = await db.Properties
            .FirstOrDefaultAsync(p => 
                p.Id == propertyId && 
                p.ApplicationUserId == userId, ct);

        if (property is null)
            throw new KeyNotFoundException("Property not found or not owned by user.");

        // Get all photos from all cameras on this property using explicit join to avoid LINQ translation errors
        var baseQuery = db.Photos
            .Join(db.Cameras, p => p.CameraId, c => c.Id, (p, c) => new { Photo = p, Camera = c })
            .Where(x => x.Camera.PropertyId == propertyId)
            .Where(x => x.Camera.Property.ApplicationUserId == userId);

        // Apply filters if provided - but we need to handle this differently with the join
        if (filters?.HasAnyFilters == true)
        {
            // Convert back to Photo query for filtering, then rejoin for final selection
            var photosQuery = db.Photos
                .Where(p => p.Camera.PropertyId == propertyId)
                .Where(p => p.Camera.Property.ApplicationUserId == userId);

            if (filters.HasWeatherFilters)
            {
                photosQuery = photosQuery.Include(p => p.Weather);
            }

            photosQuery = ApplyFilters(photosQuery, filters);

            // Now join with cameras for the final selection
            baseQuery = photosQuery
                .Join(db.Cameras, p => p.CameraId, c => c.Id, (p, c) => new { Photo = p, Camera = c });
        }

        // Apply sorting
        var sortedQuery = sortBy switch
        {
            SortBy.DateTakenAsc => baseQuery.OrderBy(x => x.Photo.DateTaken),
            SortBy.DateTakenDesc => baseQuery.OrderByDescending(x => x.Photo.DateTaken),
            SortBy.DateUploadedAsc => baseQuery.OrderBy(x => x.Photo.DateUploaded),
            SortBy.DateUploadedDesc => baseQuery.OrderByDescending(x => x.Photo.DateUploaded),
            _ => baseQuery.OrderByDescending(x => x.Photo.DateTaken)
        };

        var photos = await sortedQuery
            .Select(x => new PhotoListItem(
                x.Photo.Id,
                x.Photo.PhotoUrl,
                x.Photo.DateTaken,
                x.Photo.DateUploaded,
                x.Photo.CameraId,
                x.Camera.Name
            ))
            .ToListAsync(ct);

        return photos;
    }

    private static IQueryable<Photo> ApplyFilters(IQueryable<Photo> query, PhotoFilters filters)
    {
        // Date/Time filters
        if (filters.DateTakenFrom.HasValue)
            query = query.Where(p => p.DateTaken >= filters.DateTakenFrom.Value);
        
        if (filters.DateTakenTo.HasValue)
            query = query.Where(p => p.DateTaken <= filters.DateTakenTo.Value);
            
        if (filters.DateUploadedFrom.HasValue)
            query = query.Where(p => p.DateUploaded >= filters.DateUploadedFrom.Value);
            
        if (filters.DateUploadedTo.HasValue)
            query = query.Where(p => p.DateUploaded <= filters.DateUploadedTo.Value);

        // Camera filters
        if (filters.CameraIds?.Count > 0)
            query = query.Where(p => filters.CameraIds.Contains(p.CameraId));

        // Weather filters - only apply if photo has weather data
        if (filters.HasWeatherFilters)
        {
            if (filters.TemperatureMin.HasValue)
                query = query.Where(p => p.Weather != null && p.Weather.Temperature >= filters.TemperatureMin.Value);
                
            if (filters.TemperatureMax.HasValue)
                query = query.Where(p => p.Weather != null && p.Weather.Temperature <= filters.TemperatureMax.Value);
                
            if (filters.WindSpeedMin.HasValue)
                query = query.Where(p => p.Weather != null && p.Weather.WindSpeed >= filters.WindSpeedMin.Value);
                
            if (filters.WindSpeedMax.HasValue)
                query = query.Where(p => p.Weather != null && p.Weather.WindSpeed <= filters.WindSpeedMax.Value);
                
            if (filters.HumidityMin.HasValue)
                query = query.Where(p => p.Weather != null && p.Weather.Humidity >= filters.HumidityMin.Value);
                
            if (filters.HumidityMax.HasValue)
                query = query.Where(p => p.Weather != null && p.Weather.Humidity <= filters.HumidityMax.Value);
                
            if (filters.PressureMin.HasValue)
                query = query.Where(p => p.Weather != null && p.Weather.Pressure >= filters.PressureMin.Value);
                
            if (filters.PressureMax.HasValue)
                query = query.Where(p => p.Weather != null && p.Weather.Pressure <= filters.PressureMax.Value);
                
            if (filters.VisibilityMin.HasValue)
                query = query.Where(p => p.Weather != null && p.Weather.Visibility >= filters.VisibilityMin.Value);
                
            if (filters.VisibilityMax.HasValue)
                query = query.Where(p => p.Weather != null && p.Weather.Visibility <= filters.VisibilityMax.Value);
                
            if (filters.CloudCoverMin.HasValue)
                query = query.Where(p => p.Weather != null && p.Weather.CloudCover >= filters.CloudCoverMin.Value);
                
            if (filters.CloudCoverMax.HasValue)
                query = query.Where(p => p.Weather != null && p.Weather.CloudCover <= filters.CloudCoverMax.Value);
                
            if (filters.MoonPhaseMin.HasValue)
                query = query.Where(p => p.Weather != null && p.Weather.MoonPhase >= filters.MoonPhaseMin.Value);
                
            if (filters.MoonPhaseMax.HasValue)
                query = query.Where(p => p.Weather != null && p.Weather.MoonPhase <= filters.MoonPhaseMax.Value);

            // Categorical weather filters
            if (filters.Conditions?.Count > 0)
                query = query.Where(p => p.Weather != null && filters.Conditions.Contains(p.Weather.Conditions));
                
            if (filters.MoonPhaseTexts?.Count > 0)
                query = query.Where(p => p.Weather != null && filters.MoonPhaseTexts.Contains(p.Weather.MoonPhaseText));
                
            if (filters.PressureTrends?.Count > 0)
                query = query.Where(p => p.Weather != null && filters.PressureTrends.Contains(p.Weather.PressureTrend));
                
            if (filters.WindDirectionTexts?.Count > 0)
                query = query.Where(p => p.Weather != null && filters.WindDirectionTexts.Contains(p.Weather.WindDirectionText));
        }

        return query;
    }
}