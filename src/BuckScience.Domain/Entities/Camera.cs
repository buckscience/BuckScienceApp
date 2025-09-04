using NetTopologySuite.Geometries;

namespace BuckScience.Domain.Entities;

public class Camera
{
    protected Camera() { } // For EF

    public Camera(
        string name,
        string brand,
        string? model,
        bool isActive = true,
        DateTime? createdDate = null)
    {
        Rename(name);
        SetBrand(brand);
        SetModel(model);
        IsActive = isActive;
        CreatedDate = createdDate ?? DateTime.UtcNow;
    }

    public int Id { get; private set; }

    public string Name { get; private set; } = string.Empty;
    public string Brand { get; private set; } = string.Empty;
    public string? Model { get; private set; }

    public DateTime CreatedDate { get; private set; } = DateTime.UtcNow;
    public bool IsActive { get; private set; } = true;

    // Domain-only convenience (not persisted)
    public int PhotoCount { get; set; }

    // FK + navigation
    public int PropertyId { get; private set; }
    public virtual Property Property { get; private set; } = default!;

    public virtual ICollection<Photo> Photos { get; private set; } = new List<Photo>();
    public virtual ICollection<CameraPlacementHistory> PlacementHistories { get; private set; } = new List<CameraPlacementHistory>();

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

    public void PlaceAt(double latitude, double longitude, float directionDegrees, DateTime? placementTime = null)
    {
        var currentPlacement = GetCurrentPlacement();
        var now = placementTime ?? DateTime.UtcNow;

        // End current placement if exists
        if (currentPlacement != null)
        {
            currentPlacement.EndPlacement(now);
        }

        // Create new placement
        var newPlacement = new CameraPlacementHistory(Id, latitude, longitude, directionDegrees, now);
        PlacementHistories.Add(newPlacement);
    }

    public void Move(double latitude, double longitude, float directionDegrees)
    {
        PlaceAt(latitude, longitude, directionDegrees);
    }

    public CameraPlacementHistory? GetCurrentPlacement()
    {
        return PlacementHistories.FirstOrDefault(p => p.IsCurrentPlacement);
    }

    // Convenience accessors for current location
    public double Latitude => GetCurrentPlacement()?.Latitude ?? 0d;
    public double Longitude => GetCurrentPlacement()?.Longitude ?? 0d;
    public float DirectionDegrees => GetCurrentPlacement()?.DirectionDegrees ?? 0f;
    public Point? Location => GetCurrentPlacement()?.Location;

    public void SetActive(bool active) => IsActive = active;

    // Helper to set location from lat/lng (backwards compatibility)
    public static Point CreatePoint(double latitude, double longitude)
        => new Point(longitude, latitude) { SRID = 4326 };
}