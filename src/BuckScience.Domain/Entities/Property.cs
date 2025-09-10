using NetTopologySuite.Geometries;
using Point = NetTopologySuite.Geometries.Point;

namespace BuckScience.Domain.Entities;

public class Property
{
    protected Property() { }

    public Property(
        string name,
        Point location,
        MultiPolygon? boundary,
        string timeZone,
        int dayHour,
        int nightHour,
        DateTime? createdDate = null)
    {
        Rename(name);
        SetLocation(location);
        SetBoundary(boundary);
        TimeZone = timeZone;
        DayHour = dayHour;
        NightHour = nightHour;
        CreatedDate = createdDate ?? DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public string Name { get; private set; } = string.Empty;

    // Spatial columns (SQL Server geometry)
    public Point Center { get; private set; } = default!;
    public MultiPolygon? Boundary { get; private set; }

    public DateTime CreatedDate { get; private set; }
    public string TimeZone { get; private set; } = string.Empty;
    public int DayHour { get; private set; }
    public int NightHour { get; private set; }

    // Owner for per-user segmentation
    public int ApplicationUserId { get; private set; }

    // Convenience accessors (NTS uses X=Longitude, Y=Latitude)
    public double Latitude => Center?.Y ?? 0d;
    public double Longitude => Center?.X ?? 0d;

    // Navigation
    public virtual ICollection<Camera> Cameras { get; set; } = new HashSet<Camera>();
    public virtual ICollection<PropertyFeature> PropertyFeatures { get; set; } = new HashSet<PropertyFeature>();
    public virtual ICollection<FeatureWeight> FeatureWeights { get; set; } = new HashSet<FeatureWeight>();

    public void AssignOwner(int applicationUserId)
    {
        if (applicationUserId <= 0) throw new ArgumentOutOfRangeException(nameof(applicationUserId));
        ApplicationUserId = applicationUserId;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Name is required.", nameof(newName));
        Name = newName.Trim();
    }

    public void SetLocation(Point point)
    {
        if (point is null) throw new ArgumentNullException(nameof(point));
        if (point.SRID == 0) point.SRID = 4326;
        Center = point;
    }

    public void Move(double latitude, double longitude)
    {
        SetLocation(new Point(longitude, latitude) { SRID = 4326 });
    }

    public void SetBoundary(MultiPolygon? boundary)
    {
        if (boundary != null && boundary.SRID == 0) boundary.SRID = 4326;
        Boundary = boundary;
    }

    public void SetHours(int dayHour, int nightHour)
    {
        DayHour = dayHour;
        NightHour = nightHour;
    }
}