using NetTopologySuite.Geometries;

namespace BuckScience.Domain.Entities;

public class Camera
{
    protected Camera() { } // For EF

    public Camera(
        string name,
        string brand,
        string? model,
        Point location,
        bool isActive = true,
        DateTime? createdDate = null)
    {
        Rename(name);
        SetBrand(brand);
        SetModel(model);
        SetLocation(location);
        IsActive = isActive;
        CreatedDate = createdDate ?? DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string Brand { get; private set; } = string.Empty;
    public string? Model { get; private set; }

    // Spatial point (WGS84). X = Longitude, Y = Latitude.
    public Point Location { get; private set; } = default!;

    // Convenience accessors (NTS uses X=Longitude, Y=Latitude)
    public double Latitude => Location?.Y ?? 0d;
    public double Longitude => Location?.X ?? 0d;

    public DateTime CreatedDate { get; private set; } = DateTime.UtcNow;
    public bool IsActive { get; private set; } = true;

    // Domain-only convenience (not persisted)
    public int PhotoCount { get; set; }

    // FK + navigation
    public int PropertyId { get; private set; }
    public virtual Property Property { get; private set; } = default!;

    public virtual ICollection<Photo> Photos { get; private set; } = new List<Photo>();

    // Domain behavior
    public void PlaceInProperty(int propertyId)
    {
        if (propertyId <= 0) throw new ArgumentOutOfRangeException(nameof(propertyId));
        PropertyId = propertyId;
    }

    public void Rename(string newName)
    {
        if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Name is required.", nameof(newName));
        Name = newName.Trim();
    }

    public void SetBrand(string brand)
    {
        Brand = brand?.Trim() ?? string.Empty;
    }

    public void SetModel(string? model)
    {
        Model = string.IsNullOrWhiteSpace(model) ? null : model.Trim();
    }

    public void SetLocation(Point point)
    {
        if (point is null) throw new ArgumentNullException(nameof(point));
        if (point.SRID == 0) point.SRID = 4326;
        Location = point;
    }

    public void Move(double latitude, double longitude)
    {
        SetLocation(new Point(longitude, latitude) { SRID = 4326 });
    }

    public void SetActive(bool active) => IsActive = active;

    // Helper to set location from lat/lng
    public static Point CreatePoint(double latitude, double longitude)
        => new Point(longitude, latitude) { SRID = 4326 };
}