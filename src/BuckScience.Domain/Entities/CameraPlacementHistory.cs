using NetTopologySuite.Geometries;

namespace BuckScience.Domain.Entities;

public class CameraPlacementHistory
{
    protected CameraPlacementHistory() { } // For EF

    public CameraPlacementHistory(
        int cameraId,
        double latitude,
        double longitude,
        float directionDegrees,
        DateTime startDateTime)
    {
        CameraId = cameraId;
        Latitude = latitude;
        Longitude = longitude;
        DirectionDegrees = directionDegrees;
        StartDateTime = startDateTime;
    }

    public int Id { get; private set; }
    public int CameraId { get; private set; }
    public double Latitude { get; private set; }
    public double Longitude { get; private set; }
    public float DirectionDegrees { get; private set; } // 0-360, 0=North
    public DateTime StartDateTime { get; private set; }
    public DateTime? EndDateTime { get; private set; }

    // Navigation property
    public virtual Camera Camera { get; private set; } = default!;

    // Convenience property to create Point for spatial operations
    public Point Location => new Point(Longitude, Latitude) { SRID = 4326 };

    // Domain behavior
    public void EndPlacement(DateTime endDateTime)
    {
        if (EndDateTime.HasValue)
            throw new InvalidOperationException("Placement has already been ended.");
        
        if (endDateTime <= StartDateTime)
            throw new ArgumentException("End date must be after start date.", nameof(endDateTime));

        EndDateTime = endDateTime;
    }

    public bool IsCurrentPlacement => !EndDateTime.HasValue;

    public TimeSpan? Duration => EndDateTime?.Subtract(StartDateTime);
}