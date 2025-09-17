using BuckScience.Domain.Enums;
using NetTopologySuite.Geometries;

namespace BuckScience.Domain.Entities;

public class PropertyFeature
{
    protected PropertyFeature() { }

    public PropertyFeature(
        int propertyId,
        ClassificationType classificationType,
        Geometry geometry,
        string? name = null,
        string? notes = null,
        float? weight = null,
        int? createdBy = null,
        DateTime? createdAt = null)
    {
        PropertyId = propertyId;
        ClassificationType = classificationType;
        SetGeometry(geometry);
        Name = name;
        Notes = notes;
        SetWeight(weight);
        CreatedBy = createdBy;
        CreatedAt = createdAt ?? DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int PropertyId { get; private set; }
    public ClassificationType ClassificationType { get; private set; }
    public Geometry Geometry { get; private set; } = default!;
    public string? Name { get; private set; }
    public string? Notes { get; private set; }
    public float? Weight { get; private set; }
    public int? CreatedBy { get; private set; }
    public DateTime CreatedAt { get; private set; }

    // Navigation properties
    public virtual Property Property { get; private set; } = default!;

    public void UpdateClassificationType(ClassificationType classificationType)
    {
        ClassificationType = classificationType;
    }

    public void SetGeometry(Geometry geometry)
    {
        if (geometry is null) throw new ArgumentNullException(nameof(geometry));
        if (geometry.SRID == 0) geometry.SRID = 4326;
        Geometry = geometry;
    }

    public void UpdateNotes(string? notes)
    {
        Notes = notes;
    }

    public void UpdateName(string? name)
    {
        Name = name;
    }

    public void SetWeight(float? weight)
    {
        if (weight.HasValue && (weight.Value < 0 || weight.Value > 1))
            throw new ArgumentOutOfRangeException(nameof(weight), "Weight must be between 0 and 1.");
        Weight = weight;
    }

    public void UpdateWeight(float? weight)
    {
        SetWeight(weight);
    }

    public void AssignCreatedBy(int userId)
    {
        if (userId <= 0) throw new ArgumentOutOfRangeException(nameof(userId));
        CreatedBy = userId;
    }
}