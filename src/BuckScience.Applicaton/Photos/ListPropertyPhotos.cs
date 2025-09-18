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
        string CameraLocationName
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
            .Join(db.Properties, x => x.Camera.PropertyId, prop => prop.Id, (x, prop) => new { x.Photo, x.Camera, Property = prop })
            .Where(x => x.Camera.PropertyId == propertyId)
            .Where(x => x.Property.ApplicationUserId == userId)
            .Select(x => new { 
                Photo = x.Photo, 
                Camera = x.Camera,
                CurrentPlacement = x.Camera.PlacementHistories.Where(ph => ph.EndDateTime == null).FirstOrDefault()
            });

        // Apply filters if provided - but we need to handle this differently with the join
        if (filters?.HasAnyFilters == true)
        {
            // Convert back to Photo query for filtering using explicit joins
            var photosQuery = db.Photos
                .Join(db.Cameras, p => p.CameraId, c => c.Id, (p, c) => new { Photo = p, Camera = c })
                .Join(db.Properties, x => x.Camera.PropertyId, prop => prop.Id, (x, prop) => new { x.Photo, x.Camera, Property = prop })
                .Where(x => x.Camera.PropertyId == propertyId)
                .Where(x => x.Property.ApplicationUserId == userId)
                .Select(x => x.Photo);

            if (filters.HasWeatherFilters)
            {
                photosQuery = photosQuery.Include(p => p.Weather);
            }

            photosQuery = ApplyFilters(photosQuery, filters);

            // Now join with cameras for the final selection
            baseQuery = photosQuery
                .Join(db.Cameras, p => p.CameraId, c => c.Id, (p, c) => new { 
                    Photo = p, 
                    Camera = c,
                    CurrentPlacement = c.PlacementHistories.Where(ph => ph.EndDateTime == null).FirstOrDefault()
                });
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
                x.CurrentPlacement != null ? x.CurrentPlacement.LocationName : ""
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

        // Time of day filters
        if (filters.TimeOfDayStart.HasValue && filters.TimeOfDayEnd.HasValue)
        {
            var startHour = filters.TimeOfDayStart.Value;
            var endHour = filters.TimeOfDayEnd.Value;
            
            if (startHour <= endHour)
            {
                // Normal range (e.g., 6 AM to 6 PM)
                query = query.Where(p => p.DateTaken.Hour >= startHour && p.DateTaken.Hour < endHour);
            }
            else
            {
                // Overnight range (e.g., 6 PM to 6 AM)
                query = query.Where(p => p.DateTaken.Hour >= startHour || p.DateTaken.Hour < endHour);
            }
        }

        // Camera filters - use Contains for simple integer list filtering
        if (filters.CameraIds?.Count > 0)
        {
            query = query.Where(p => filters.CameraIds.Contains(p.CameraId));
        }

        // Camera placement history filters - use Contains for placement history ID filtering
        if (filters.CameraPlacementHistoryIds?.Count > 0)
        {
            query = query.Where(p => p.CameraPlacementHistoryId.HasValue && 
                                   filters.CameraPlacementHistoryIds.Contains(p.CameraPlacementHistoryId.Value));
        }

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

            // Categorical weather filters - using explicit joins to avoid Contains translation errors
            if (filters.Conditions?.Count > 0)
            {
                var filteredConditions = filters.Conditions.AsQueryable();
                query = query.Where(p => p.Weather != null)
                           .Join(filteredConditions, p => p.Weather!.Conditions, c => c, (p, c) => p);
            }
                
            if (filters.MoonPhaseTexts?.Count > 0)
            {
                var filteredMoonPhases = filters.MoonPhaseTexts.AsQueryable();
                query = query.Where(p => p.Weather != null)
                           .Join(filteredMoonPhases, p => p.Weather!.MoonPhaseText, m => m, (p, m) => p);
            }
                
            if (filters.PressureTrends?.Count > 0)
            {
                var filteredPressureTrends = filters.PressureTrends.AsQueryable();
                query = query.Where(p => p.Weather != null)
                           .Join(filteredPressureTrends, p => p.Weather!.PressureTrend, pt => pt, (p, pt) => p);
            }
                
            if (filters.WindDirectionTexts?.Count > 0)
            {
                var filteredWindDirections = filters.WindDirectionTexts.AsQueryable();
                query = query.Where(p => p.Weather != null)
                           .Join(filteredWindDirections, p => p.Weather!.WindDirectionText, wd => wd, (p, wd) => p);
            }
        }

        return query;
    }
}