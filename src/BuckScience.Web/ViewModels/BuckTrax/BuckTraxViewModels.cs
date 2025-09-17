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
}

public class BuckTraxPredictionResult
{
    public int ProfileId { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string PropertyName { get; set; } = string.Empty;
    public int TotalSightings { get; set; }
    public DateTime PredictionDate { get; set; }
    public List<BuckTraxTimeSegmentPrediction> TimeSegments { get; set; } = new();
}

public class BuckTraxTimeSegmentPrediction
{
    public string TimeSegment { get; set; } = string.Empty;
    public int StartHour { get; set; }
    public int EndHour { get; set; }
    public int SightingCount { get; set; }
    public double ConfidenceScore { get; set; }
    public List<BuckTraxPredictionZone> PredictedZones { get; set; } = new();
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
}

public class BuckTraxSighting
{
    public int PhotoId { get; set; }
    public DateTime DateTaken { get; set; }
    public int CameraId { get; set; }
    public string CameraName { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
}

public class BuckTraxFeature
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public int ClassificationType { get; set; }
    public GeometryType GeometryType { get; set; }
    public string Coordinates { get; set; } = string.Empty;
}