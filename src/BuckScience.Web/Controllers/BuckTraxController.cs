using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.FeatureWeights;
using BuckScience.Application.Profiles;
using BuckScience.Domain.Enums;
using BuckScience.Web.ViewModels.BuckTrax;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Web.Controllers;

[Authorize]
public class BuckTraxController : Controller
{
    private readonly IAppDbContext _db;
    private readonly ICurrentUserService _currentUser;

    public BuckTraxController(IAppDbContext db, ICurrentUserService currentUser)
    {
        _db = db;
        _currentUser = currentUser;
    }

    // INDEX: GET /bucktrax
    [HttpGet]
    [Route("/bucktrax")]
    public async Task<IActionResult> Index(CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        // Get all properties for the current user
        var properties = await _db.Properties
            .AsNoTracking()
            .Where(p => p.ApplicationUserId == _currentUser.Id.Value)
            .OrderBy(p => p.Name)
            .Select(p => new BuckTraxPropertyVm
            {
                Id = p.Id,
                Name = p.Name
            })
            .ToListAsync(ct);

        var vm = new BuckTraxIndexVm
        {
            Properties = properties
        };

        return View(vm);
    }

    // API: GET /bucktrax/api/properties/{propertyId}/profiles
    [HttpGet]
    [Route("/bucktrax/api/properties/{propertyId}/profiles")]
    public async Task<IActionResult> GetPropertyProfiles(int propertyId, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            var profiles = await ListPropertyProfiles.HandleAsync(_db, _currentUser.Id.Value, propertyId, ct);
            
            var result = profiles.Select(p => new BuckTraxProfileVm
            {
                Id = p.Id,
                Name = p.Name,
                ProfileStatus = p.ProfileStatus,
                TagName = p.TagName,
                CoverPhotoUrl = p.CoverPhotoUrl
            }).ToList();

            return Json(result);
        }
        catch (Exception)
        {
            return NotFound();
        }
    }

    // API: GET /bucktrax/api/configuration
    [HttpGet]
    [Route("/bucktrax/api/configuration")]
    public async Task<IActionResult> GetConfiguration(CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        var config = GetConfiguration();
        return Json(config);
    }

    // API: POST /bucktrax/api/predict
    [HttpPost]
    [Route("/bucktrax/api/predict")]
    public async Task<IActionResult> PredictMovement([FromBody] BuckTraxPredictionRequest request, CancellationToken ct)
    {
        if (_currentUser.Id is null) return Forbid();

        try
        {
            // Validate profile ownership
            var profile = await GetProfile.HandleAsync(request.ProfileId, _db, _currentUser.Id.Value, ct);
            if (profile == null) return NotFound("Profile not found");

            // Generate movement predictions with corridor analysis
            var predictions = await GenerateMovementPredictions(request.ProfileId, request.Season, ct);

            return Json(predictions);
        }
        catch (UnauthorizedAccessException)
        {
            return Forbid();
        }
        catch (Exception ex)
        {
            return BadRequest($"Error generating predictions: {ex.Message}");
        }
    }

    private async Task<BuckTraxPredictionResult> GenerateMovementPredictions(int profileId, Season? season, CancellationToken ct)
    {
        var config = GetConfiguration();
        
        // Get profile data including sightings and property features
        var profile = await _db.Profiles
            .AsNoTracking()
            .Include(p => p.Property)
            .FirstAsync(p => p.Id == profileId, ct);

        // Get all sightings for this profile, ordered chronologically
        var sightings = await GetProfileSightings(profileId, profile.TagId, profile.PropertyId, ct);
        
        // Get property features with effective weights
        var features = await GetPropertyFeaturesWithWeights(profile.PropertyId, season, ct);
        
        // Associate each sighting with its nearest feature
        var sightingsWithFeatures = AssociateSightingsWithFeatures(sightings, features, config.CameraFeatureProximityMeters);
        
        // Analyze movement corridors from sequential sightings
        var movementCorridors = AnalyzeMovementCorridors(sightingsWithFeatures, features, config);
        
        // Check if we have sufficient data
        var isLimitedData = sightings.Count < config.MinimumSightingsThreshold || 
                           movementCorridors.Count < config.MinimumTransitionsThreshold;

        // Time segments for prediction
        var timeSegments = new[]
        {
            new { Name = "Early Morning", Start = 5, End = 8 },
            new { Name = "Morning", Start = 8, End = 11 },
            new { Name = "Midday", Start = 11, End = 14 },
            new { Name = "Afternoon", Start = 14, End = 17 },
            new { Name = "Evening", Start = 17, End = 20 },
            new { Name = "Night", Start = 20, End = 5 }
        };

        var predictions = new List<BuckTraxTimeSegmentPrediction>();

        foreach (var segment in timeSegments)
        {
            // Filter sightings for this time segment
            var segmentSightings = sightingsWithFeatures.Where(s => 
            {
                var hour = s.DateTaken.Hour;
                if (segment.Start <= segment.End)
                {
                    return hour >= segment.Start && hour < segment.End;
                }
                else // Night segment spans midnight
                {
                    return hour >= segment.Start || hour < segment.End;
                }
            }).ToList();

            // Filter corridors for this time segment
            var segmentCorridors = movementCorridors.Where(c => 
                IsCorridorActiveInTimeSegment(c, segment.Start, segment.End)).ToList();

            if (segmentSightings.Any() || segmentCorridors.Any())
            {
                // Calculate probability zones based on historical sightings and corridors
                var zones = CalculateEnhancedPredictionZones(segmentSightings, segmentCorridors, features);
                
                predictions.Add(new BuckTraxTimeSegmentPrediction
                {
                    TimeSegment = segment.Name,
                    StartHour = segment.Start,
                    EndHour = segment.End,
                    SightingCount = segmentSightings.Count,
                    PredictedZones = zones,
                    TimeSegmentCorridors = segmentCorridors,
                    ConfidenceScore = CalculateConfidenceScore(segmentSightings.Count, sightings.Count, segmentCorridors.Count)
                });
            }
            else
            {
                predictions.Add(new BuckTraxTimeSegmentPrediction
                {
                    TimeSegment = segment.Name,
                    StartHour = segment.Start,
                    EndHour = segment.End,
                    SightingCount = 0,
                    PredictedZones = new List<BuckTraxPredictionZone>(),
                    TimeSegmentCorridors = new List<BuckTraxMovementCorridor>(),
                    ConfidenceScore = 0
                });
            }
        }

        return new BuckTraxPredictionResult
        {
            ProfileId = profileId,
            ProfileName = profile.Name,
            PropertyName = profile.Property.Name,
            TotalSightings = sightings.Count,
            TotalTransitions = movementCorridors.Sum(c => c.TransitionCount),
            PredictionDate = DateTime.UtcNow,
            IsLimitedData = isLimitedData,
            LimitedDataMessage = isLimitedData && config.ShowLimitedDataWarning 
                ? "Due to limited data, the predictive model is extremely limited." 
                : null,
            TimeSegments = predictions,
            MovementCorridors = movementCorridors,
            Configuration = config
        };
    }

    private async Task<List<BuckTraxSighting>> GetProfileSightings(int profileId, int tagId, int propertyId, CancellationToken ct)
    {
        return await _db.Photos
            .AsNoTracking()
            .Where(p => _db.PhotoTags.Any(pt => pt.PhotoId == p.Id && pt.TagId == tagId))
            .Join(_db.Cameras, p => p.CameraId, c => c.Id, (p, c) => new { p, c })
            .Where(pc => pc.c.PropertyId == propertyId)
            .Select(pc => new { 
                Photo = pc.p, 
                Camera = pc.c,
                CurrentPlacement = pc.c.PlacementHistories
                    .Where(ph => ph.EndDateTime == null)
                    .FirstOrDefault()
            })
            .Where(pc => pc.CurrentPlacement != null)
            .Select(pc => new BuckTraxSighting
            {
                PhotoId = pc.Photo.Id,
                DateTaken = pc.Photo.DateTaken,
                CameraId = pc.Camera.Id,
                CameraName = pc.CurrentPlacement!.LocationName,
                Latitude = pc.CurrentPlacement.Latitude,
                Longitude = pc.CurrentPlacement.Longitude
            })
            .OrderBy(s => s.DateTaken)
            .ToListAsync(ct);
    }

    private async Task<List<BuckTraxFeature>> GetPropertyFeaturesWithWeights(int propertyId, Season? season, CancellationToken ct)
    {
        // Get all property features with their weights
        var featureWeights = await GetEffectiveFeatureWeight.GetAllPropertyFeatureWeightsAsync(_db, propertyId, season, ct);
        
        var features = await _db.PropertyFeatures
            .AsNoTracking()
            .Where(f => f.PropertyId == propertyId)
            .ToListAsync(ct);

        return features.Select(f => new BuckTraxFeature
        {
            Id = f.Id,
            Name = f.Name ?? "",
            ClassificationType = (int)f.ClassificationType,
            ClassificationName = f.ClassificationType.ToString(),
            GeometryType = GetGeometryType(f.Geometry),
            Coordinates = f.Geometry.ToString() ?? "",
            Latitude = GetLatitudeFromGeometry(f.Geometry),
            Longitude = GetLongitudeFromGeometry(f.Geometry),
            EffectiveWeight = featureWeights.GetValueOrDefault(f.Id, 0.5f)
        }).ToList();
    }

    private List<BuckTraxSighting> AssociateSightingsWithFeatures(
        List<BuckTraxSighting> sightings, 
        List<BuckTraxFeature> features, 
        double proximityThreshold)
    {
        foreach (var sighting in sightings)
        {
            var nearestFeature = features
                .Select(f => new { 
                    Feature = f, 
                    Distance = CalculateDistance(sighting.Latitude, sighting.Longitude, f.Latitude, f.Longitude) 
                })
                .Where(x => x.Distance <= proximityThreshold)
                .OrderBy(x => x.Distance)
                .FirstOrDefault();

            if (nearestFeature != null)
            {
                sighting.AssociatedFeatureId = nearestFeature.Feature.Id;
                sighting.AssociatedFeatureName = nearestFeature.Feature.Name;
            }
        }

        return sightings;
    }

    private List<BuckTraxMovementCorridor> AnalyzeMovementCorridors(
        List<BuckTraxSighting> sightings, 
        List<BuckTraxFeature> features, 
        BuckTraxConfiguration config)
    {
        var corridors = new Dictionary<string, BuckTraxMovementCorridor>();
        var featureLookup = features.ToDictionary(f => f.Id, f => f);

        // Analyze sequential sightings for movement patterns
        for (int i = 0; i < sightings.Count - 1; i++)
        {
            var currentSighting = sightings[i];
            var nextSighting = sightings[i + 1];

            // Check if both sightings are associated with features
            if (!currentSighting.AssociatedFeatureId.HasValue || !nextSighting.AssociatedFeatureId.HasValue)
                continue;

            // Check time window constraint
            var timeDiff = nextSighting.DateTaken - currentSighting.DateTaken;
            if (timeDiff.TotalMinutes > config.MovementTimeWindowMinutes)
                continue;

            // Check distance constraint
            var distance = CalculateDistance(
                currentSighting.Latitude, currentSighting.Longitude,
                nextSighting.Latitude, nextSighting.Longitude);
            
            if (distance > config.MaxMovementDistanceMeters)
                continue;

            // Skip same feature transitions
            if (currentSighting.AssociatedFeatureId == nextSighting.AssociatedFeatureId)
                continue;

            // Create corridor key
            var startFeatureId = currentSighting.AssociatedFeatureId.Value;
            var endFeatureId = nextSighting.AssociatedFeatureId.Value;
            var corridorKey = $"{Math.Min(startFeatureId, endFeatureId)}-{Math.Max(startFeatureId, endFeatureId)}";

            if (!corridors.ContainsKey(corridorKey))
            {
                var startFeature = featureLookup[startFeatureId];
                var endFeature = featureLookup[endFeatureId];

                corridors[corridorKey] = new BuckTraxMovementCorridor
                {
                    Name = $"{startFeature.Name} → {endFeature.Name}",
                    StartFeatureId = startFeatureId,
                    EndFeatureId = endFeatureId,
                    StartFeatureName = startFeature.Name,
                    EndFeatureName = endFeature.Name,
                    StartFeatureType = startFeature.ClassificationName,
                    EndFeatureType = endFeature.ClassificationName,
                    StartLatitude = startFeature.Latitude,
                    StartLongitude = startFeature.Longitude,
                    EndLatitude = endFeature.Latitude,
                    EndLongitude = endFeature.Longitude,
                    TransitionCount = 0,
                    StartFeatureWeight = startFeature.EffectiveWeight,
                    EndFeatureWeight = endFeature.EffectiveWeight,
                    Distance = distance,
                    AverageTimeSpan = 0,
                    TimeOfDayPattern = ""
                };
            }

            // Update corridor statistics
            var corridor = corridors[corridorKey];
            corridor.TransitionCount++;
            
            // Update average time span
            var totalTime = corridor.AverageTimeSpan * (corridor.TransitionCount - 1) + timeDiff.TotalHours;
            corridor.AverageTimeSpan = totalTime / corridor.TransitionCount;

            // Calculate corridor score using feature weights
            corridor.CorridorScore = corridor.TransitionCount * (corridor.StartFeatureWeight + corridor.EndFeatureWeight) / 2;
        }

        // Calculate time of day patterns for each corridor
        foreach (var corridor in corridors.Values)
        {
            var transitionTimes = new List<int>();
            
            for (int i = 0; i < sightings.Count - 1; i++)
            {
                var currentSighting = sightings[i];
                var nextSighting = sightings[i + 1];

                if (currentSighting.AssociatedFeatureId == corridor.StartFeatureId &&
                    nextSighting.AssociatedFeatureId == corridor.EndFeatureId)
                {
                    transitionTimes.Add(currentSighting.DateTaken.Hour);
                }
            }

            corridor.TimeOfDayPattern = GetTimeOfDayPattern(transitionTimes);
        }

        return corridors.Values.OrderByDescending(c => c.CorridorScore).ToList();
    }

    private List<BuckTraxPredictionZone> CalculateEnhancedPredictionZones(
        List<BuckTraxSighting> sightings, 
        List<BuckTraxMovementCorridor> corridors,
        List<BuckTraxFeature> features)
    {
        var zones = new List<BuckTraxPredictionZone>();

        // Add zones from historical sightings
        var sightingsByLocation = sightings
            .GroupBy(s => new { s.CameraId, s.CameraName, s.Latitude, s.Longitude, s.AssociatedFeatureId })
            .OrderByDescending(g => g.Count())
            .ToList();

        foreach (var locationGroup in sightingsByLocation)
        {
            var probability = (double)locationGroup.Count() / Math.Max(sightings.Count, 1);
            var associatedFeature = locationGroup.Key.AssociatedFeatureId.HasValue 
                ? features.FirstOrDefault(f => f.Id == locationGroup.Key.AssociatedFeatureId.Value) 
                : null;
            
            zones.Add(new BuckTraxPredictionZone
            {
                LocationName = locationGroup.Key.CameraName,
                Latitude = locationGroup.Key.Latitude,
                Longitude = locationGroup.Key.Longitude,
                Probability = probability,
                SightingCount = locationGroup.Count(),
                RadiusMeters = probability > 0.5 ? 100 : probability > 0.25 ? 200 : 300,
                IsCorridorPrediction = false,
                AssociatedFeatureId = associatedFeature?.Id,
                FeatureType = associatedFeature?.ClassificationName,
                FeatureWeight = associatedFeature?.EffectiveWeight
            });
        }

        // Add zones from movement corridors
        foreach (var corridor in corridors.Take(5)) // Top 5 corridors
        {
            var corridorProbability = Math.Min(corridor.CorridorScore / 10.0, 0.8); // Scale and cap probability
            
            // Add start point
            zones.Add(new BuckTraxPredictionZone
            {
                LocationName = $"{corridor.StartFeatureName} (Corridor Start)",
                Latitude = corridor.StartLatitude,
                Longitude = corridor.StartLongitude,
                Probability = corridorProbability,
                SightingCount = corridor.TransitionCount,
                RadiusMeters = 150,
                IsCorridorPrediction = true,
                AssociatedFeatureId = corridor.StartFeatureId,
                FeatureType = corridor.StartFeatureType,
                FeatureWeight = corridor.StartFeatureWeight
            });

            // Add end point
            zones.Add(new BuckTraxPredictionZone
            {
                LocationName = $"{corridor.EndFeatureName} (Corridor End)",
                Latitude = corridor.EndLatitude,
                Longitude = corridor.EndLongitude,
                Probability = corridorProbability,
                SightingCount = corridor.TransitionCount,
                RadiusMeters = 150,
                IsCorridorPrediction = true,
                AssociatedFeatureId = corridor.EndFeatureId,
                FeatureType = corridor.EndFeatureType,
                FeatureWeight = corridor.EndFeatureWeight
            });
        }

        return zones.OrderByDescending(z => z.Probability).ToList();
    }

    private double CalculateConfidenceScore(int segmentSightings, int totalSightings, int corridorCount = 0)
    {
        if (totalSightings == 0) return 0;
        
        var proportion = (double)segmentSightings / totalSightings;
        var dataConfidence = Math.Min(segmentSightings / 10.0, 1.0);
        var proportionConfidence = proportion;
        var corridorBonus = Math.Min(corridorCount / 5.0, 0.2); // Up to 20% bonus for corridors
        
        var baseScore = (dataConfidence * 0.6 + proportionConfidence * 0.4) * 100;
        return Math.Round(Math.Min(baseScore + (corridorBonus * 100), 100), 1);
    }

    private bool IsCorridorActiveInTimeSegment(BuckTraxMovementCorridor corridor, int startHour, int endHour)
    {
        // Simple pattern matching - could be enhanced with more sophisticated analysis
        if (string.IsNullOrEmpty(corridor.TimeOfDayPattern))
            return true;

        // Check if the corridor's primary activity time overlaps with the segment
        if (corridor.TimeOfDayPattern.Contains("Morning") && startHour >= 6 && endHour <= 12)
            return true;
        if (corridor.TimeOfDayPattern.Contains("Evening") && startHour >= 16 && endHour <= 21)
            return true;
        if (corridor.TimeOfDayPattern.Contains("Night") && (startHour >= 20 || endHour <= 6))
            return true;

        return true; // Default to including all corridors
    }

    private string GetTimeOfDayPattern(List<int> transitionTimes)
    {
        if (!transitionTimes.Any())
            return "Unknown";

        var patterns = new List<string>();
        
        if (transitionTimes.Count(t => t >= 5 && t < 8) > transitionTimes.Count * 0.3)
            patterns.Add("Early Morning");
        if (transitionTimes.Count(t => t >= 8 && t < 12) > transitionTimes.Count * 0.3)
            patterns.Add("Morning");
        if (transitionTimes.Count(t => t >= 12 && t < 17) > transitionTimes.Count * 0.3)
            patterns.Add("Afternoon");
        if (transitionTimes.Count(t => t >= 17 && t < 20) > transitionTimes.Count * 0.3)
            patterns.Add("Evening");
        if (transitionTimes.Count(t => t >= 20 || t < 5) > transitionTimes.Count * 0.3)
            patterns.Add("Night");

        return patterns.Any() ? string.Join(", ", patterns) : "All Day";
    }

    private GeometryType GetGeometryType(Geometry geometry)
    {
        return geometry switch
        {
            Point => GeometryType.Point,
            LineString => GeometryType.Polyline,
            Polygon => GeometryType.Polygon,
            MultiPoint => GeometryType.MultiPoint,
            MultiLineString => GeometryType.MultiPolyline,
            MultiPolygon => GeometryType.MultiPolygon,
            _ => GeometryType.Point
        };
    }

    private double GetLatitudeFromGeometry(Geometry geometry)
    {
        return geometry switch
        {
            Point point => point.Y,
            _ => geometry.Centroid?.Y ?? 0
        };
    }

    private double GetLongitudeFromGeometry(Geometry geometry)
    {
        return geometry switch
        {
            Point point => point.X,
            _ => geometry.Centroid?.X ?? 0
        };
    }

    private double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double R = 6371000; // Earth's radius in meters
        var dLat = ToRadians(lat2 - lat1);
        var dLon = ToRadians(lon2 - lon1);
        var a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                Math.Cos(ToRadians(lat1)) * Math.Cos(ToRadians(lat2)) *
                Math.Sin(dLon / 2) * Math.Sin(dLon / 2);
        var c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));
        return R * c;
    }

    private double ToRadians(double degrees)
    {
        return degrees * Math.PI / 180;
    }

    private BuckTraxConfiguration GetConfiguration()
    {
        return new BuckTraxConfiguration
        {
            MovementTimeWindowMinutes = 480, // 8 hours
            MaxMovementDistanceMeters = 5000, // 5 km
            CameraFeatureProximityMeters = 100, // 100 meters
            MinimumSightingsThreshold = 10,
            MinimumTransitionsThreshold = 3,
            ShowLimitedDataWarning = true
        };
    }
}