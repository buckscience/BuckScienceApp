using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.Profiles;
using BuckScience.Domain.Enums;
using BuckScience.Web.ViewModels.BuckTrax;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

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

            // For now, implement a basic movement prediction
            // This is where the "secret sauce" logic would go
            var predictions = await GenerateMovementPredictions(request.ProfileId, ct);

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

    private async Task<BuckTraxPredictionResult> GenerateMovementPredictions(int profileId, CancellationToken ct)
    {
        // Get profile data including sightings and property features
        var profile = await _db.Profiles
            .AsNoTracking()
            .Include(p => p.Property)
            .FirstAsync(p => p.Id == profileId, ct);

        // Get tagged photos (sightings) with camera location data
        var sightings = await _db.Photos
            .AsNoTracking()
            .Where(p => _db.PhotoTags.Any(pt => pt.PhotoId == p.Id && pt.TagId == profile.TagId))
            .Join(_db.Cameras, p => p.CameraId, c => c.Id, (p, c) => new { p, c })
            .Where(pc => pc.c.PropertyId == profile.PropertyId)
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

        // Get property features for movement corridors
        var features = await _db.PropertyFeatures
            .AsNoTracking()
            .Where(f => f.PropertyId == profile.PropertyId)
            .Select(f => new BuckTraxFeature
            {
                Id = f.Id,
                Name = f.Name ?? "",
                ClassificationType = (int)f.ClassificationType,
                GeometryType = GeometryType.Point, // Simplified for now
                Coordinates = f.Geometry.ToString() ?? ""
            })
            .ToListAsync(ct);

        // Time segments for prediction (Early Morning, Morning, Midday, Afternoon, Evening, Night)
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
            var segmentSightings = sightings.Where(s => 
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

            if (segmentSightings.Any())
            {
                // Calculate probability zones based on historical sightings
                var zones = CalculatePredictionZones(segmentSightings, features);
                
                predictions.Add(new BuckTraxTimeSegmentPrediction
                {
                    TimeSegment = segment.Name,
                    StartHour = segment.Start,
                    EndHour = segment.End,
                    SightingCount = segmentSightings.Count,
                    PredictedZones = zones,
                    ConfidenceScore = CalculateConfidenceScore(segmentSightings.Count, sightings.Count)
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
            PredictionDate = DateTime.UtcNow,
            TimeSegments = predictions
        };
    }

    private List<BuckTraxPredictionZone> CalculatePredictionZones(List<BuckTraxSighting> sightings, List<BuckTraxFeature> features)
    {
        var zones = new List<BuckTraxPredictionZone>();

        // Group sightings by camera location
        var sightingsByLocation = sightings
            .GroupBy(s => new { s.CameraId, s.CameraName, s.Latitude, s.Longitude })
            .OrderByDescending(g => g.Count())
            .ToList();

        foreach (var locationGroup in sightingsByLocation)
        {
            var probability = (double)locationGroup.Count() / sightings.Count;
            
            zones.Add(new BuckTraxPredictionZone
            {
                LocationName = locationGroup.Key.CameraName,
                Latitude = locationGroup.Key.Latitude,
                Longitude = locationGroup.Key.Longitude,
                Probability = probability,
                SightingCount = locationGroup.Count(),
                // Add buffer radius based on confidence
                RadiusMeters = probability > 0.5 ? 100 : probability > 0.25 ? 200 : 300
            });
        }

        // Add corridor predictions based on features
        var corridorFeatures = features.Where(f => IsMovementCorridor(f.ClassificationType)).ToList();
        foreach (var feature in corridorFeatures.Take(3)) // Top 3 corridor features
        {
            // Extract centroid from coordinates for visualization
            if (TryExtractCentroid(feature.Coordinates, out var lat, out var lng))
            {
                zones.Add(new BuckTraxPredictionZone
                {
                    LocationName = feature.Name,
                    Latitude = lat,
                    Longitude = lng,
                    Probability = 0.3, // Base probability for corridor features
                    SightingCount = 0,
                    RadiusMeters = 150,
                    IsCorridorPrediction = true
                });
            }
        }

        return zones.OrderByDescending(z => z.Probability).ToList();
    }

    private double CalculateConfidenceScore(int segmentSightings, int totalSightings)
    {
        if (totalSightings == 0) return 0;
        
        var proportion = (double)segmentSightings / totalSightings;
        // Confidence increases with more data points and higher proportion
        var dataConfidence = Math.Min(segmentSightings / 10.0, 1.0);
        var proportionConfidence = proportion;
        
        return Math.Round((dataConfidence * 0.6 + proportionConfidence * 0.4) * 100, 1);
    }

    private bool IsMovementCorridor(int classificationType)
    {
        // Based on ClassificationType enum values, these are movement-related features
        return classificationType switch
        {
            6 => true,  // Draw
            7 => true,  // CreekCrossing
            11 => true, // FieldEdge
            15 => true, // PinchPointFunnel
            16 => true, // TravelCorridor
            _ => false
        };
    }

    private bool TryExtractCentroid(string geometryString, out double lat, out double lng)
    {
        lat = 0;
        lng = 0;

        try
        {
            // NetTopologySuite geometries are serialized differently
            // This is a simplified extraction - in practice you'd use proper geometry parsing
            if (geometryString.Contains("POINT") && geometryString.Contains('(') && geometryString.Contains(')'))
            {
                var start = geometryString.IndexOf('(');
                var end = geometryString.IndexOf(')', start);
                if (start >= 0 && end > start)
                {
                    var coordPart = geometryString.Substring(start + 1, end - start - 1).Trim();
                    var parts = coordPart.Split(' ');
                    if (parts.Length >= 2 &&
                        double.TryParse(parts[0].Trim(), out lng) &&
                        double.TryParse(parts[1].Trim(), out lat))
                    {
                        return true;
                    }
                }
            }
            else if (geometryString.Contains('[') && geometryString.Contains(']'))
            {
                // Fallback for JSON-like coordinates
                var start = geometryString.IndexOf('[');
                var end = geometryString.IndexOf(']', start);
                if (start >= 0 && end > start)
                {
                    var coordPart = geometryString.Substring(start + 1, end - start - 1);
                    var parts = coordPart.Split(',');
                    if (parts.Length >= 2 &&
                        double.TryParse(parts[0].Trim(), out lng) &&
                        double.TryParse(parts[1].Trim(), out lat))
                    {
                        return true;
                    }
                }
            }
        }
        catch
        {
            // Ignore parsing errors
        }

        return false;
    }
}