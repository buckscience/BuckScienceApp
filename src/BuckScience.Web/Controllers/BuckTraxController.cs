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

        // Calculate time segments based on property's daylight hours
        var dayStart = profile.Property.DayHour;
        var dayEnd = profile.Property.NightHour;
        
        // Calculate daylight span and split into thirds
        int daylightSpan;
        if (dayEnd > dayStart)
        {
            daylightSpan = dayEnd - dayStart;
        }
        else
        {
            // Handle case where night hour is next day (e.g., day=6, night=20 means 6AM-8PM = 14 hours)
            daylightSpan = (24 - dayStart) + dayEnd;
        }
        
        var thirdSpan = (double)daylightSpan / 3.0;
        
        // Calculate time segment boundaries
        var morningStart = dayStart;
        var morningEnd = (int)Math.Round(dayStart + thirdSpan) % 24;
        var afternoonStart = morningEnd;
        var afternoonEnd = (int)Math.Round(dayStart + (2 * thirdSpan)) % 24;
        var eveningStart = afternoonEnd;
        var eveningEnd = dayEnd;

        // Time segments for prediction based on property daylight hours
        var timeSegments = new[]
        {
            new { Name = "Morning", Start = morningStart, End = morningEnd },
            new { Name = "Afternoon", Start = afternoonStart, End = afternoonEnd },
            new { Name = "Evening", Start = eveningStart, End = eveningEnd },
            new { Name = "Night", Start = dayEnd, End = dayStart }
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
        // Get all tagged photos first
        var allTaggedPhotos = await _db.Photos
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

        // Group photos into sightings: photos within 15 minutes at the same camera = one sighting
        return GroupPhotosIntoSightings(allTaggedPhotos);
    }

    /// <summary>
    /// Groups photos into sightings based on camera location and time proximity.
    /// Photos taken at the same camera within 15 minutes of each other are considered one sighting.
    /// </summary>
    private List<BuckTraxSighting> GroupPhotosIntoSightings(List<BuckTraxSighting> allPhotos)
    {
        var sightings = new List<BuckTraxSighting>();
        
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
        return sightings.OrderBy(s => s.DateTaken).ToList();
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
        var corridors = new List<BuckTraxMovementCorridor>();
        var featureLookup = features.ToDictionary(f => f.Id, f => f);

        // First, identify movement routes (sequences of locations within time windows)
        var routes = IdentifyMovementRoutes(sightings, config);

        // Convert routes to corridors
        foreach (var route in routes)
        {
            if (route.Points.Count < 2) continue;

            if (route.Points.Count == 2)
            {
                // Simple point-to-point corridor
                var corridor = CreateSimpleCorridor(route.Points[0], route.Points[1], featureLookup, route.Id);
                corridors.Add(corridor);
            }
            else
            {
                // Multi-point route - create a primary corridor representing the full route
                var primaryCorridor = CreateMultiPointCorridor(route, featureLookup);
                corridors.Add(primaryCorridor);
            }
        }

        // Calculate time of day patterns for each corridor
        foreach (var corridor in corridors)
        {
            corridor.TimeOfDayPattern = CalculateTimeOfDayPattern(corridor, sightings);
        }

        return corridors.OrderByDescending(c => c.CorridorScore).ToList();
    }

    private List<MovementRoute> IdentifyMovementRoutes(List<BuckTraxSighting> sightings, BuckTraxConfiguration config)
    {
        var routes = new List<MovementRoute>();
        var routeId = 1;

        for (int i = 0; i < sightings.Count; i++)
        {
            var route = new MovementRoute { Id = $"route-{routeId++}" };
            var currentSighting = sightings[i];
            
            // Add first point
            route.Points.Add(new RoutePoint
            {
                Order = 1,
                Sighting = currentSighting,
                LocationId = GetLocationId(currentSighting),
                LocationName = GetLocationName(currentSighting),
                LocationType = GetLocationType(currentSighting),
                Latitude = currentSighting.Latitude,
                Longitude = currentSighting.Longitude,
                VisitTime = currentSighting.DateTaken
            });

            // Look ahead for connected movements within the time window
            for (int j = i + 1; j < sightings.Count; j++)
            {
                var nextSighting = sightings[j];
                var lastPoint = route.Points.Last();
                
                var timeDiff = nextSighting.DateTaken - lastPoint.VisitTime;
                if (timeDiff.TotalMinutes > config.MovementTimeWindowMinutes)
                    break; // Time window exceeded

                var distance = CalculateDistance(
                    lastPoint.Latitude, lastPoint.Longitude,
                    nextSighting.Latitude, nextSighting.Longitude);
                
                if (distance > config.MaxMovementDistanceMeters)
                    continue; // Too far

                // Skip same location
                if (GetLocationId(nextSighting) == lastPoint.LocationId)
                    continue;

                // Check for barriers
                if (IsMovementBlocked(sightings[j-1], nextSighting, new List<BuckTraxFeature>()))
                    continue;

                // Add this point to the route
                route.Points.Add(new RoutePoint
                {
                    Order = route.Points.Count + 1,
                    Sighting = nextSighting,
                    LocationId = GetLocationId(nextSighting),
                    LocationName = GetLocationName(nextSighting),
                    LocationType = GetLocationType(nextSighting),
                    Latitude = nextSighting.Latitude,
                    Longitude = nextSighting.Longitude,
                    VisitTime = nextSighting.DateTaken
                });

                i = j; // Skip processed sightings
            }

            // Only add routes with movement (more than one point)
            if (route.Points.Count > 1)
            {
                routes.Add(route);
            }
        }

        return routes;
    }

    private BuckTraxMovementCorridor CreateSimpleCorridor(RoutePoint start, RoutePoint end, Dictionary<int, BuckTraxFeature> featureLookup, string routeId)
    {
        var distance = CalculateDistance(start.Latitude, start.Longitude, end.Latitude, end.Longitude);
        var timeDiff = end.VisitTime - start.VisitTime;
        
        // Get weights from features if available
        var startWeight = GetLocationWeight(start, featureLookup);
        var endWeight = GetLocationWeight(end, featureLookup);
        
        return new BuckTraxMovementCorridor
        {
            Name = $"{start.LocationName} → {end.LocationName}",
            StartFeatureId = start.LocationId,
            EndFeatureId = end.LocationId,
            StartFeatureName = start.LocationName,
            EndFeatureName = end.LocationName,
            StartFeatureType = start.LocationType,
            EndFeatureType = end.LocationType,
            StartLatitude = start.Latitude,
            StartLongitude = start.Longitude,
            EndLatitude = end.Latitude,
            EndLongitude = end.Longitude,
            TransitionCount = 1,
            StartFeatureWeight = startWeight,
            EndFeatureWeight = endWeight,
            Distance = distance,
            AverageTimeSpan = timeDiff.TotalHours,
            RouteId = routeId,
            RoutePoints = new List<BuckTraxRoutePoint>
            {
                new BuckTraxRoutePoint
                {
                    Order = 1,
                    LocationId = start.LocationId,
                    LocationName = start.LocationName,
                    LocationType = start.LocationType,
                    Latitude = start.Latitude,
                    Longitude = start.Longitude,
                    VisitTime = start.VisitTime
                },
                new BuckTraxRoutePoint
                {
                    Order = 2,
                    LocationId = end.LocationId,
                    LocationName = end.LocationName,
                    LocationType = end.LocationType,
                    Latitude = end.Latitude,
                    Longitude = end.Longitude,
                    VisitTime = end.VisitTime
                }
            },
            IsPartOfMultiPointRoute = false,
            CorridorScore = CalculateCorridorScore(1, startWeight, endWeight)
        };
    }

    private BuckTraxMovementCorridor CreateMultiPointCorridor(MovementRoute route, Dictionary<int, BuckTraxFeature> featureLookup)
    {
        var firstPoint = route.Points.First();
        var lastPoint = route.Points.Last();
        var totalDistance = 0.0;
        var totalTime = (lastPoint.VisitTime - firstPoint.VisitTime).TotalHours;
        
        // Calculate total distance along the route
        for (int i = 0; i < route.Points.Count - 1; i++)
        {
            var current = route.Points[i];
            var next = route.Points[i + 1];
            totalDistance += CalculateDistance(current.Latitude, current.Longitude, next.Latitude, next.Longitude);
        }

        var startWeight = GetLocationWeight(firstPoint, featureLookup);
        var endWeight = GetLocationWeight(lastPoint, featureLookup);
        
        // Create route name showing the path
        var routeName = string.Join(" → ", route.Points.Select(p => p.LocationName));
        
        return new BuckTraxMovementCorridor
        {
            Name = routeName,
            StartFeatureId = firstPoint.LocationId,
            EndFeatureId = lastPoint.LocationId,
            StartFeatureName = firstPoint.LocationName,
            EndFeatureName = lastPoint.LocationName,
            StartFeatureType = firstPoint.LocationType,
            EndFeatureType = lastPoint.LocationType,
            StartLatitude = firstPoint.Latitude,
            StartLongitude = firstPoint.Longitude,
            EndLatitude = lastPoint.Latitude,
            EndLongitude = lastPoint.Longitude,
            TransitionCount = route.Points.Count - 1, // Number of transitions
            StartFeatureWeight = startWeight,
            EndFeatureWeight = endWeight,
            Distance = totalDistance,
            AverageTimeSpan = totalTime,
            RouteId = route.Id,
            RoutePoints = route.Points.Select(p => new BuckTraxRoutePoint
            {
                Order = p.Order,
                LocationId = p.LocationId,
                LocationName = p.LocationName,
                LocationType = p.LocationType,
                Latitude = p.Latitude,
                Longitude = p.Longitude,
                VisitTime = p.VisitTime
            }).ToList(),
            IsPartOfMultiPointRoute = true,
            CorridorScore = CalculateCorridorScore(route.Points.Count - 1, startWeight, endWeight) * 1.5 // Boost score for multi-point routes
        };
    }

    private int GetLocationId(BuckTraxSighting sighting)
    {
        return sighting.AssociatedFeatureId ?? sighting.CameraId;
    }

    private string GetLocationName(BuckTraxSighting sighting)
    {
        return sighting.AssociatedFeatureName ?? sighting.CameraName;
    }

    private string GetLocationType(BuckTraxSighting sighting)
    {
        return sighting.AssociatedFeatureId.HasValue ? "Property Feature" : "Camera Location";
    }

    private float GetLocationWeight(RoutePoint point, Dictionary<int, BuckTraxFeature> featureLookup)
    {
        if (point.LocationType == "Property Feature" && featureLookup.ContainsKey(point.LocationId))
        {
            return featureLookup[point.LocationId].EffectiveWeight;
        }
        return 0.5f; // Default weight for camera locations
    }

    private double CalculateCorridorScore(int transitionCount, float startWeight, float endWeight)
    {
        var weightMultiplier = (startWeight + endWeight) / 2;
        var amplifiedWeight = Math.Max(0.5, Math.Pow(weightMultiplier, 1.5));
        return transitionCount * amplifiedWeight * 5;
    }

    private string CalculateTimeOfDayPattern(BuckTraxMovementCorridor corridor, List<BuckTraxSighting> sightings)
    {
        var transitionTimes = new List<int>();
        
        // For multi-point routes, use the start time of the route
        if (corridor.IsPartOfMultiPointRoute && corridor.RoutePoints.Any())
        {
            transitionTimes.Add(corridor.RoutePoints.First().VisitTime.Hour);
        }
        else
        {
            // For simple corridors, find matching transitions in the sightings
            for (int i = 0; i < sightings.Count - 1; i++)
            {
                var currentSighting = sightings[i];
                var nextSighting = sightings[i + 1];

                bool isMatchingTransition = false;
                
                if (corridor.StartFeatureType == "Camera Location")
                {
                    isMatchingTransition = (currentSighting.CameraId == corridor.StartFeatureId && 
                                          nextSighting.CameraId == corridor.EndFeatureId) ||
                                         (currentSighting.CameraId == corridor.EndFeatureId && 
                                          nextSighting.CameraId == corridor.StartFeatureId);
                }
                else
                {
                    isMatchingTransition = (currentSighting.AssociatedFeatureId == corridor.StartFeatureId &&
                                          nextSighting.AssociatedFeatureId == corridor.EndFeatureId) ||
                                         (currentSighting.AssociatedFeatureId == corridor.EndFeatureId &&
                                          nextSighting.AssociatedFeatureId == corridor.StartFeatureId);
                }

                if (isMatchingTransition)
                {
                    transitionTimes.Add(currentSighting.DateTaken.Hour);
                }
            }
        }

        return GetTimeOfDayPattern(transitionTimes);
    }

    // Helper classes for route identification
    private class MovementRoute
    {
        public string Id { get; set; } = string.Empty;
        public List<RoutePoint> Points { get; set; } = new();
    }

    private class RoutePoint
    {
        public int Order { get; set; }
        public BuckTraxSighting Sighting { get; set; } = null!;
        public int LocationId { get; set; }
        public string LocationName { get; set; } = string.Empty;
        public string LocationType { get; set; } = string.Empty;
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public DateTime VisitTime { get; set; }
    }

    private bool IsMovementBlocked(BuckTraxSighting from, BuckTraxSighting to, List<BuckTraxFeature> features)
    {
        // Calculate bearing to detect major barriers
        var bearing = CalculateBearing(from.Latitude, from.Longitude, to.Latitude, to.Longitude);
        var distance = CalculateDistance(from.Latitude, from.Longitude, to.Latitude, to.Longitude);
        
        // Block movements that are too straight-line over long distances (likely roads/highways)
        if (distance > 1000 && IsStraightLineMovement(bearing, distance))
        {
            return true;
        }
        
        // Block movements across water features unless they're specifically crossings
        if (CrossesWaterBarrier(from, to, features))
        {
            return true;
        }
        
        return false;
    }

    private double CalculateBearing(double lat1, double lon1, double lat2, double lon2)
    {
        var dLon = ToRadians(lon2 - lon1);
        var lat1Rad = ToRadians(lat1);
        var lat2Rad = ToRadians(lat2);

        var y = Math.Sin(dLon) * Math.Cos(lat2Rad);
        var x = Math.Cos(lat1Rad) * Math.Sin(lat2Rad) - Math.Sin(lat1Rad) * Math.Cos(lat2Rad) * Math.Cos(dLon);

        var bearing = Math.Atan2(y, x);
        return (bearing * 180 / Math.PI + 360) % 360;
    }

    private bool IsStraightLineMovement(double bearing, double distance)
    {
        // Flag perfectly straight movements over long distances as potentially crossing roads
        // Allow some variance for natural movement patterns
        return distance > 1500; // Flag movements over 1.5km as potentially problematic
    }

    private bool CrossesWaterBarrier(BuckTraxSighting from, BuckTraxSighting to, List<BuckTraxFeature> features)
    {
        // Check if movement crosses large water bodies (not including crossings)
        var waterFeatures = features.Where(f => 
            f.ClassificationName.Contains("Lake") || 
            f.ClassificationName.Contains("Pond") ||
            (f.ClassificationName.Contains("Creek") && !f.ClassificationName.Contains("Crossing")))
            .ToList();

        // Simplified check - could be enhanced with proper geometric intersection
        foreach (var water in waterFeatures)
        {
            var distanceToWater = Math.Min(
                CalculateDistance(from.Latitude, from.Longitude, water.Latitude, water.Longitude),
                CalculateDistance(to.Latitude, to.Longitude, water.Latitude, water.Longitude)
            );
            
            // If movement passes very close to water features, consider it blocked
            if (distanceToWater < 50) // 50 meters
            {
                return true;
            }
        }

        return false;
    }

    private List<BuckTraxPredictionZone> CalculateEnhancedPredictionZones(
        List<BuckTraxSighting> sightings, 
        List<BuckTraxMovementCorridor> corridors,
        List<BuckTraxFeature> features)
    {
        var zones = new List<BuckTraxPredictionZone>();

        // Add zones from historical sightings with feature weight influence
        var sightingsByLocation = sightings
            .GroupBy(s => new { s.CameraId, s.CameraName, s.Latitude, s.Longitude, s.AssociatedFeatureId })
            .OrderByDescending(g => g.Count())
            .ToList();

        foreach (var locationGroup in sightingsByLocation)
        {
            var baseFrequency = (double)locationGroup.Count() / Math.Max(sightings.Count, 1);
            var associatedFeature = locationGroup.Key.AssociatedFeatureId.HasValue 
                ? features.FirstOrDefault(f => f.Id == locationGroup.Key.AssociatedFeatureId.Value) 
                : null;
            
            // Apply feature weight boost to probability
            var featureWeightMultiplier = associatedFeature?.EffectiveWeight ?? 0.5f;
            var weightAdjustedProbability = baseFrequency * (1 + featureWeightMultiplier);
            
            zones.Add(new BuckTraxPredictionZone
            {
                LocationName = locationGroup.Key.CameraName,
                Latitude = locationGroup.Key.Latitude,
                Longitude = locationGroup.Key.Longitude,
                Probability = Math.Min(weightAdjustedProbability, 1.0), // Cap at 100%
                SightingCount = locationGroup.Count(),
                RadiusMeters = weightAdjustedProbability > 0.6 ? 80 : weightAdjustedProbability > 0.3 ? 150 : 250,
                IsCorridorPrediction = false,
                AssociatedFeatureId = associatedFeature?.Id,
                FeatureType = associatedFeature?.ClassificationName,
                FeatureWeight = associatedFeature?.EffectiveWeight
            });
        }

        // Add zones from movement corridors with enhanced weighting
        foreach (var corridor in corridors.Take(8)) // Increased to show more corridors
        {
            // Enhanced corridor probability calculation using feature weights
            var baseCorridorProbability = Math.Min(corridor.CorridorScore / 15.0, 0.9); // Adjusted scaling
            var weightBonus = (corridor.StartFeatureWeight + corridor.EndFeatureWeight) / 4; // Additional weight bonus
            var finalProbability = Math.Min(baseCorridorProbability + weightBonus, 0.95);
            
            // Add start point with weight influence
            zones.Add(new BuckTraxPredictionZone
            {
                LocationName = $"{corridor.StartFeatureName} (Corridor Entry)",
                Latitude = corridor.StartLatitude,
                Longitude = corridor.StartLongitude,
                Probability = finalProbability,
                SightingCount = corridor.TransitionCount,
                RadiusMeters = (int)(100 + (corridor.StartFeatureWeight * 100)), // Radius based on weight
                IsCorridorPrediction = true,
                AssociatedFeatureId = corridor.StartFeatureId,
                FeatureType = corridor.StartFeatureType,
                FeatureWeight = corridor.StartFeatureWeight
            });

            // Add end point with weight influence  
            zones.Add(new BuckTraxPredictionZone
            {
                LocationName = $"{corridor.EndFeatureName} (Corridor Exit)",
                Latitude = corridor.EndLatitude,
                Longitude = corridor.EndLongitude,
                Probability = finalProbability,
                SightingCount = corridor.TransitionCount,
                RadiusMeters = (int)(100 + (corridor.EndFeatureWeight * 100)), // Radius based on weight
                IsCorridorPrediction = true,
                AssociatedFeatureId = corridor.EndFeatureId,
                FeatureType = corridor.EndFeatureType,
                FeatureWeight = corridor.EndFeatureWeight
            });
        }

        // Add predictions for high-weight features even without historical data
        var highWeightFeatures = features.Where(f => f.EffectiveWeight > 0.7f).ToList();
        foreach (var feature in highWeightFeatures)
        {
            // Only add if not already covered by sightings or corridors
            var alreadyCovered = zones.Any(z => z.AssociatedFeatureId == feature.Id);
            if (!alreadyCovered)
            {
                zones.Add(new BuckTraxPredictionZone
                {
                    LocationName = $"{feature.Name} (High Priority Feature)",
                    Latitude = feature.Latitude,
                    Longitude = feature.Longitude,
                    Probability = feature.EffectiveWeight * 0.6, // Base probability from weight
                    SightingCount = 0,
                    RadiusMeters = (int)(120 + (feature.EffectiveWeight * 80)),
                    IsCorridorPrediction = false,
                    AssociatedFeatureId = feature.Id,
                    FeatureType = feature.ClassificationName,
                    FeatureWeight = feature.EffectiveWeight
                });
            }
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

    private GeometryType GetGeometryType(Geometry? geometry)
    {
        if (geometry == null) return GeometryType.Point;
        
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

    private double GetLatitudeFromGeometry(Geometry? geometry)
    {
        if (geometry == null) return 0;
        
        return geometry switch
        {
            Point point => point.Y,
            _ => geometry.Centroid?.Y ?? 0
        };
    }

    private double GetLongitudeFromGeometry(Geometry? geometry)
    {
        if (geometry == null) return 0;
        
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

    // REMOVED: Custom BuckTrax features API endpoint 
    // Now using standard /properties/{propertyId}/features endpoint instead
    // to avoid reinventing the wheel and ensure consistency

    private BuckTraxConfiguration GetConfiguration()
    {
        return new BuckTraxConfiguration
        {
            MovementTimeWindowMinutes = 60, // Reduced to 1 hour to capture short movements like 12 minutes
            MaxMovementDistanceMeters = 2000, // Reduced to 2 km for more realistic movement  
            CameraFeatureProximityMeters = 100, // 100 meters
            MinimumSightingsThreshold = 5, // Reduced threshold for better responsiveness
            MinimumTransitionsThreshold = 2, // Reduced threshold for better responsiveness
            ShowLimitedDataWarning = true
        };
    }
}