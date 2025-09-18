using BuckScience.Domain.Enums;

namespace BuckScience.Web.ViewModels.BuckTrax;

public class BuckTraxIndexVm
{
    public List<BuckTraxPropertyVm> Properties { get; set; } = new();
}

public class BuckTraxPropertyVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class BuckTraxProfileVm
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public ProfileStatus ProfileStatus { get; set; }
    public string TagName { get; set; } = string.Empty;
    public string? CoverPhotoUrl { get; set; }
}

public class BuckTraxPredictionRequest
{
    public int ProfileId { get; set; }
    public Season? Season { get; set; }
    public int? TimeOfDayFilter { get; set; } // 0-23 hour filter
}

public class BuckTraxPredictionResult
{
    public int ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public int TotalSightings { get; set; }
    public int TotalTransitions { get; set; }
    public DateTime PredictionDate { get; set; }
    public bool IsLimitedData { get; set; }
    public string? LimitedDataMessage { get; set; }
    public List<BuckTraxTimeSegmentPrediction> TimeSegments { get; set; } = new();
    public List<BuckTraxMovementCorridor> MovementCorridors { get; set; } = new();
    public BuckTraxConfiguration Configuration { get; set; } = new();
    public int DefaultTimeSegmentIndex { get; set; } = 0; // Index of the most active time segment for default selection
}

public class BuckTraxTimeSegmentPrediction
{
    public string TimeSegment { get; set; } = string.Empty;
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public int SightingCount { get; set; }
    public double ConfidenceScore { get; set; }
    public List<BuckTraxPredictionZone> PredictedZones { get; set; } = new();
    public List<BuckTraxMovementCorridor> TimeSegmentCorridors { get; set; } = new();
}

public class BuckTraxPredictionZone
{
    public string LocationName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double Probability { get; set; }
    public int SightingCount { get; set; }
    public double RadiusMeters { get; set; }
    public bool IsCorridorPrediction { get; set; }
    public int? AssociatedFeatureId { get; set; }
    public string? FeatureType { get; set; }
    public float? FeatureWeight { get; set; }
}

public class BuckTraxMovementCorridor
{
    public string Name { get; set; } = string.Empty;
    public int StartFeatureId { get; set; }
    public int EndFeatureId { get; set; }
    public string StartFeatureName { get; set; } = string.Empty;
    public string EndFeatureName { get; set; } = string.Empty;
    public string StartFeatureType { get; set; } = string.Empty;
    public string EndFeatureType { get; set; } = string.Empty;
    public double StartLatitude { get; set; }
    public double StartLongitude { get; set; }
    public double EndLatitude { get; set; }
    public double EndLongitude { get; set; }
    public int TransitionCount { get; set; }
    public double CorridorScore { get; set; }
    public float StartFeatureWeight { get; set; }
    public float EndFeatureWeight { get; set; }
    public double Distance { get; set; }
    public double AverageTimeSpan { get; set; }
    public string TimeOfDayPattern { get; set; } = string.Empty;
    
    // New properties for route support
    public string? RouteId { get; set; } = null; // Groups corridors that are part of the same route
    public List<BuckTraxRoutePoint> RoutePoints { get; set; } = new(); // Ordered points for multi-point routes
    public bool IsPartOfMultiPointRoute { get; set; } = false;
}

public class BuckTraxRoutePoint
{
    public int Order { get; set; } // 1, 2, 3, 4...
    public int LocationId { get; set; } // Camera ID or Feature ID
    public string LocationName { get; set; } = string.Empty;
    public string LocationType { get; set; } = string.Empty; // "Camera Location" or feature type
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public DateTime VisitTime { get; set; }
}

// Helper classes for route identification (moved from private to support testing)
public class MovementRoute
{
    public string Id { get; set; } = string.Empty;
    public List<RoutePoint> Points { get; set; } = new();
}

public class RoutePoint
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

public class BuckTraxSighting
{
    public int PhotoId { get; set; }
    public DateTime DateTaken { get; set; }
    public int CameraId { get; set; }
    public string CameraName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int? AssociatedFeatureId { get; set; }
    public string? AssociatedFeatureName { get; set; } = string.Empty;
}

public class BuckTraxFeature
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ClassificationType { get; set; }
    public string ClassificationName { get; set; } = string.Empty;
    public GeometryType GeometryType { get; set; }
    public string Coordinates { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public float EffectiveWeight { get; set; }
}

public class BuckTraxConfiguration
{
    public int MovementTimeWindowMinutes { get; set; } = 480; // 8 hours
    public double MaxMovementDistanceMeters { get; set; } = 5000; // 5 km
    public double CameraFeatureProximityMeters { get; set; } = 100; // 100 meters
    public int MinimumSightingsThreshold { get; set; } = 10;
    public int MinimumTransitionsThreshold { get; set; } = 3;
    public bool ShowLimitedDataWarning { get; set; } = true;
    
    // New configuration options for feature-aware routing
    public bool EnableFeatureAwareRouting { get; set; } = true;
    public double MinimumDistanceForFeatureRouting { get; set; } = 200; // Don't use feature routing for very short distances
    public double MaximumDetourPercentage { get; set; } = 0.3; // 30% longer route allowed for feature routing
    public int MaximumWaypointsPerRoute { get; set; } = 2; // Limit waypoints to avoid overly complex routes
}