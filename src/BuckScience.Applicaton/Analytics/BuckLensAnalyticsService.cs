using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using Microsoft.EntityFrameworkCore;
using System.Globalization;

namespace BuckScience.Application.Analytics;

public class BuckLensAnalyticsService
{
    private readonly IAppDbContext _db;

    public BuckLensAnalyticsService(IAppDbContext db)
    {
        _db = db;
    }

    public async Task<ProfileAnalyticsData> GetProfileAnalyticsAsync(int profileId, int userId, CancellationToken cancellationToken = default)
    {
        // Get profile to verify ownership
        var profile = await _db.Profiles
            .Where(p => p.Id == profileId)
            .Join(_db.Properties, p => p.PropertyId, prop => prop.Id, (p, prop) => new { p, prop })
            .Where(pp => pp.prop.ApplicationUserId == userId)
            .Select(pp => new { pp.p.TagId, pp.p.PropertyId, pp.prop.Name })
            .FirstOrDefaultAsync(cancellationToken);

        if (profile == null)
        {
            throw new UnauthorizedAccessException("Profile not found or access denied");
        }

        // Get all tagged photos with associated camera and weather data, ordered by camera and time
        var allTaggedPhotos = await _db.Photos
            .Where(p => _db.PhotoTags.Any(pt => pt.PhotoId == p.Id && pt.TagId == profile.TagId))
            .Join(_db.Cameras, p => p.CameraId, c => c.Id, (p, c) => new { p, c })
            .Where(pc => pc.c.PropertyId == profile.PropertyId)
            .Select(pc => new SightingData
            {
                PhotoId = pc.p.Id,
                DateTaken = pc.p.DateTaken,
                CameraId = pc.c.Id,
                CameraName = pc.c.PlacementHistories
                    .Where(ph => ph.EndDateTime == null)
                    .Select(ph => ph.LocationName)
                    .FirstOrDefault() ?? $"{pc.c.Brand} {pc.c.Model}".Trim(),
                Latitude = pc.c.PlacementHistories
                    .Where(ph => ph.EndDateTime == null)
                    .Select(ph => (double?)ph.Latitude)
                    .FirstOrDefault(),
                Longitude = pc.c.PlacementHistories
                    .Where(ph => ph.EndDateTime == null)
                    .Select(ph => (double?)ph.Longitude)
                    .FirstOrDefault(),
                WeatherId = pc.p.WeatherId,
                Temperature = pc.p.Weather != null ? pc.p.Weather.Temperature : (double?)null,
                WindSpeed = pc.p.Weather != null ? pc.p.Weather.WindSpeed : (double?)null,
                WindDirection = pc.p.Weather != null ? pc.p.Weather.WindDirection : (double?)null,
                WindDirectionText = pc.p.Weather != null ? pc.p.Weather.WindDirectionText : null,
                MoonPhase = pc.p.Weather != null ? pc.p.Weather.MoonPhase : (double?)null,
                MoonPhaseText = pc.p.Weather != null ? pc.p.Weather.MoonPhaseText : null,
                Conditions = pc.p.Weather != null ? pc.p.Weather.Conditions : null,
                Humidity = pc.p.Weather != null ? pc.p.Weather.Humidity : (double?)null,
                Pressure = pc.p.Weather != null ? pc.p.Weather.Pressure : (double?)null,
                CloudCover = pc.p.Weather != null ? pc.p.Weather.CloudCover : (double?)null,
                Visibility = pc.p.Weather != null ? pc.p.Weather.Visibility : (double?)null
            })
            .OrderBy(s => s.CameraId)
            .ThenBy(s => s.DateTaken)
            .ToListAsync(cancellationToken);

        // Group photos into sightings: photos within 15 minutes at the same camera = one sighting
        var sightings = GroupPhotosIntoSightings(allTaggedPhotos);

        return new ProfileAnalyticsData
        {
            ProfileId = profileId,
            PropertyName = profile.Name,
            Sightings = sightings,
            TotalSightings = sightings.Count,
            TotalTaggedPhotos = allTaggedPhotos.Count, // Total photos, not sightings
            DateRange = sightings.Any() ? 
                new DateRange(sightings.Min(s => s.DateTaken), sightings.Max(s => s.DateTaken)) : 
                new DateRange(DateTime.UtcNow, DateTime.UtcNow)
        };
    }

    public ChartData GetSightingsByCameraChart(ProfileAnalyticsData data)
    {
        var cameraGroups = data.Sightings
            .GroupBy(s => new { s.CameraId, s.CameraName })
            .Select(g => new ChartDataPoint
            {
                Label = g.Key.CameraName ?? $"Camera {g.Key.CameraId}",
                Value = g.Count(),
                Metadata = new Dictionary<string, object> { ["cameraId"] = g.Key.CameraId }
            })
            .OrderByDescending(c => c.Value)
            .ToList();

        return new ChartData
        {
            Type = "pie",
            Title = "Sightings by Camera",
            DataPoints = cameraGroups
        };
    }

    public ChartData GetSightingsByTimeOfDayChart(ProfileAnalyticsData data)
    {
        var timeGroups = data.Sightings
            .GroupBy(s => GetTimeOfDaySegment(s.DateTaken))
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = g.Count(),
                Metadata = new Dictionary<string, object> { ["segment"] = g.Key }
            })
            .OrderBy(c => GetTimeOfDayOrder(c.Label))
            .ToList();

        return new ChartData
        {
            Type = "pie",
            Title = "Sightings by Time of Day",
            DataPoints = timeGroups
        };
    }

    public ChartData GetSightingsByMoonPhaseChart(ProfileAnalyticsData data)
    {
        // Define all 8 moon phases in order
        var allMoonPhases = new[]
        {
            "New Moon", "Waxing Crescent", "First Quarter", "Waxing Gibbous",
            "Full Moon", "Waning Gibbous", "Last Quarter", "Waning Crescent"
        };

        // Get sightings grouped by moon phase
        var sightingsByPhase = data.Sightings
            .Where(s => !string.IsNullOrEmpty(s.MoonPhaseText))
            .GroupBy(s => s.MoonPhaseText!)
            .ToDictionary(g => g.Key, g => new { Count = g.Count(), AvgValue = g.Average(s => s.MoonPhase ?? 0) });

        // Create data points for all moon phases, including those with 0 sightings
        var moonPhaseGroups = allMoonPhases.Select(phase => new ChartDataPoint
        {
            Label = phase,
            Value = sightingsByPhase.ContainsKey(phase) ? sightingsByPhase[phase].Count : 0,
            Metadata = new Dictionary<string, object> 
            { 
                ["moonPhase"] = phase,
                ["avgMoonPhaseValue"] = sightingsByPhase.ContainsKey(phase) ? sightingsByPhase[phase].AvgValue : 0
            }
        }).ToList();

        return new ChartData
        {
            Type = "bar",
            Title = "Sightings by Moon Phase",
            DataPoints = moonPhaseGroups
        };
    }

    public ChartData GetSightingsByWindDirectionChart(ProfileAnalyticsData data)
    {
        var windGroups = data.Sightings
            .Where(s => !string.IsNullOrEmpty(s.WindDirectionText) && s.WindSpeed.HasValue)
            .GroupBy(s => s.WindDirectionText!)
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = g.Count(),
                Metadata = new Dictionary<string, object> 
                { 
                    ["windDirection"] = g.Key,
                    ["avgWindSpeed"] = Math.Round(g.Average(s => s.WindSpeed ?? 0), 1),
                    ["maxWindSpeed"] = g.Max(s => s.WindSpeed ?? 0),
                    ["speedRanges"] = GetWindSpeedRanges(g.ToList())
                }
            })
            .OrderByDescending(c => c.Value)
            .ToList();

        return new ChartData
        {
            Type = "radar",
            Title = "Movement by Wind Direction & Speed",
            DataPoints = windGroups
        };
    }

    private Dictionary<string, int> GetWindSpeedRanges(List<SightingData> sightings)
    {
        var ranges = new Dictionary<string, int>
        {
            ["< 1 mph"] = 0,
            ["1-4 mph"] = 0,
            ["4-8 mph"] = 0,
            ["8-12 mph"] = 0,
            ["12-16 mph"] = 0,
            ["16-20 mph"] = 0,
            ["> 20 mph"] = 0
        };

        foreach (var sighting in sightings)
        {
            if (!sighting.WindSpeed.HasValue) continue;
            
            var speed = sighting.WindSpeed.Value;
            var range = speed switch
            {
                < 1 => "< 1 mph",
                >= 1 and < 4 => "1-4 mph",
                >= 4 and < 8 => "4-8 mph",
                >= 8 and < 12 => "8-12 mph",
                >= 12 and < 16 => "12-16 mph",
                >= 16 and < 20 => "16-20 mph",
                _ => "> 20 mph"
            };
            
            ranges[range]++;
        }

        return ranges;
    }

    public ChartData GetSightingsByTemperatureChart(ProfileAnalyticsData data)
    {
        var tempGroups = data.Sightings
            .Where(s => s.Temperature.HasValue)
            .GroupBy(s => GetTemperatureBin(s.Temperature!.Value))
            .Select(g => new ChartDataPoint
            {
                Label = g.Key,
                Value = g.Count(),
                Metadata = new Dictionary<string, object> 
                { 
                    ["temperatureBin"] = g.Key,
                    ["avgTemperature"] = g.Average(s => s.Temperature!.Value)
                }
            })
            .OrderBy(c => double.Parse(c.Label.Split('-')[0]))
            .ToList();

        return new ChartData
        {
            Type = "bar",
            Title = "Sightings by Temperature",
            DataPoints = tempGroups
        };
    }

    public BestOddsAnalysis GetBestOddsAnalysis(ProfileAnalyticsData data)
    {
        if (!data.Sightings.Any())
        {
            return new BestOddsAnalysis
            {
                Summary = "No sightings data available for analysis.",
                BestTimeOfDay = null,
                BestCamera = null,
                BestMoonPhase = null,
                BestWindDirection = null,
                BestTemperatureRange = null
            };
        }

        var bestTimeOfDay = data.Sightings
            .GroupBy(s => GetTimeOfDaySegment(s.DateTaken))
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        var bestCamera = data.Sightings
            .GroupBy(s => s.CameraName)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        var bestMoonPhase = data.Sightings
            .Where(s => !string.IsNullOrEmpty(s.MoonPhaseText))
            .GroupBy(s => s.MoonPhaseText!)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        var bestWindDirection = data.Sightings
            .Where(s => !string.IsNullOrEmpty(s.WindDirectionText))
            .GroupBy(s => s.WindDirectionText!)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        var bestTempRange = data.Sightings
            .Where(s => s.Temperature.HasValue)
            .GroupBy(s => GetTemperatureBin(s.Temperature!.Value))
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        var summary = GenerateBestOddsSummary(data.TotalSightings, data.TotalTaggedPhotos, 
            bestTimeOfDay, bestCamera, bestMoonPhase, bestWindDirection, bestTempRange);

        return new BestOddsAnalysis
        {
            Summary = summary,
            BestTimeOfDay = bestTimeOfDay,
            BestCamera = bestCamera,
            BestMoonPhase = bestMoonPhase,
            BestWindDirection = bestWindDirection,
            BestTemperatureRange = bestTempRange
        };
    }

    private string GetTimeOfDaySegment(DateTime dateTime)
    {
        var hour = dateTime.Hour;
        return hour switch
        {
            >= 5 and < 10 => "Morning",
            >= 10 and < 15 => "Midday",
            >= 15 and < 20 => "Evening",
            _ => "Night"
        };
    }

    private int GetTimeOfDayOrder(string segment)
    {
        return segment switch
        {
            "Morning" => 1,
            "Midday" => 2,
            "Evening" => 3,
            "Night" => 4,
            _ => 5
        };
    }

    private string GetTemperatureBin(double temperature)
    {
        var tempF = temperature * 9 / 5 + 32; // Convert to Fahrenheit
        var binStart = Math.Floor(tempF / 5) * 5;
        return $"{binStart:F0}-{binStart + 5:F0}°F";
    }

    private string GenerateBestOddsSummary(int totalSightings, int totalPhotos, 
        string? bestTimeOfDay, string? bestCamera, string? bestMoonPhase, 
        string? bestWindDirection, string? bestTempRange)
    {
        var summary = $"You currently have <b>{totalSightings}</b> sightings from <b>{totalPhotos}</b> tagged photos.";
        
        if (totalSightings > 0)
        {
            var conditions = new List<string>();
            
            if (!string.IsNullOrEmpty(bestTimeOfDay))
                conditions.Add($"during <b>{bestTimeOfDay.ToLower()}</b>");
            
            if (!string.IsNullOrEmpty(bestCamera))
                conditions.Add($"at the <b>{bestCamera}</b> camera");
            
            if (!string.IsNullOrEmpty(bestWindDirection))
                conditions.Add($"when the wind is from the <b>{bestWindDirection}</b>");
            
            if (!string.IsNullOrEmpty(bestMoonPhase))
                conditions.Add($"during a <b>{bestMoonPhase}</b>");

            if (conditions.Any())
            {
                summary += $" Based on your data, the best odds for a sighting are {string.Join(", ", conditions)}.";
            }
        }

        return summary;
    }

    /// <summary>
    /// Groups photos into sightings based on camera location and time proximity.
    /// Photos taken at the same camera within 15 minutes of each other are considered one sighting.
    /// </summary>
    private List<SightingData> GroupPhotosIntoSightings(List<SightingData> allPhotos)
    {
        var sightings = new List<SightingData>();
        
        // Group by camera first
        var photosByCamera = allPhotos.GroupBy(p => p.CameraId);
        
        foreach (var cameraGroup in photosByCamera)
        {
            // Sort photos by time for this camera
            var sortedPhotos = cameraGroup.OrderBy(p => p.DateTaken).ToList();
            
            if (!sortedPhotos.Any()) continue;
            
            // Start first sighting with first photo
            var currentSighting = sortedPhotos[0];
            var currentSightingStartTime = currentSighting.DateTaken;
            
            for (int i = 1; i < sortedPhotos.Count; i++)
            {
                var photo = sortedPhotos[i];
                var timeDiff = photo.DateTaken - currentSightingStartTime;
                
                // If more than 15 minutes have passed, this is a new sighting
                if (timeDiff.TotalMinutes > 15)
                {
                    // Add the current sighting to results
                    sightings.Add(currentSighting);
                    
                    // Start new sighting with this photo
                    currentSighting = photo;
                    currentSightingStartTime = photo.DateTaken;
                }
                // else: this photo is part of the current sighting, no action needed
                // (we keep the first photo of the sighting as the representative)
            }
            
            // Don't forget to add the last sighting for this camera
            sightings.Add(currentSighting);
        }
        
        // Sort all sightings by date (most recent first) for consistent display
        return sightings.OrderByDescending(s => s.DateTaken).ToList();
    }
}

public class ProfileAnalyticsData
{
    public int ProfileId { get; set; }
    public string PropertyName { get; set; } = string.Empty;
    public List<SightingData> Sightings { get; set; } = new();
    public int TotalSightings { get; set; }
    public int TotalTaggedPhotos { get; set; }
    public DateRange DateRange { get; set; } = new(DateTime.UtcNow, DateTime.UtcNow);
}

public class SightingData
{
    public int PhotoId { get; set; }
    public DateTime DateTaken { get; set; }
    public int CameraId { get; set; }
    public string? CameraName { get; set; }
    public double? Latitude { get; set; }
    public double? Longitude { get; set; }
    public int? WeatherId { get; set; }
    public double? Temperature { get; set; }
    public double? WindSpeed { get; set; }
    public double? WindDirection { get; set; }
    public string? WindDirectionText { get; set; }
    public double? MoonPhase { get; set; }
    public string? MoonPhaseText { get; set; }
    public string? Conditions { get; set; }
    public double? Humidity { get; set; }
    public double? Pressure { get; set; }
    public double? CloudCover { get; set; }
    public double? Visibility { get; set; }
}

public class ChartData
{
    public string Type { get; set; } = string.Empty;
    public string Title { get; set; } = string.Empty;
    public List<ChartDataPoint> DataPoints { get; set; } = new();
}

public class ChartDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
    public Dictionary<string, object> Metadata { get; set; } = new();
}

public class BestOddsAnalysis
{
    public string Summary { get; set; } = string.Empty;
    public string? BestTimeOfDay { get; set; }
    public string? BestCamera { get; set; }
    public string? BestMoonPhase { get; set; }
    public string? BestWindDirection { get; set; }
    public string? BestTemperatureRange { get; set; }
}

public record DateRange(DateTime Start, DateTime End);