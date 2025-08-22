using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace BuckScience.Domain.Entities
{
    public class Camera
    {
        protected Camera() { } // For EF

        public Camera(string name, Point location, int propertyId, DateTime? createdDate = null)
        {
            Rename(name);
            SetLocation(location);
            AssignProperty(propertyId);
            CreatedDate = createdDate ?? DateTime.UtcNow;
            IsActive = true;
        }

        public int Id { get; private set; }

        public string Name { get; private set; } = string.Empty;
        public string Brand { get; private set; } = string.Empty;
        public string? Model { get; private set; }

        // Spatial point (SRID 4326). Note: NTS uses X=Longitude, Y=Latitude.
        public Point Location { get; private set; } = default!;

        // Convenience accessors
        public double Latitude => Location?.Y ?? 0d;
        public double Longitude => Location?.X ?? 0d;

        public DateTime CreatedDate { get; private set; }
        public bool IsActive { get; private set; }

        // Domain-only convenience (not persisted)
        public int PhotoCount { get; set; }

        // Relationships
        public int PropertyId { get; private set; }
        public virtual Property Property { get; private set; } = default!;

        public virtual ICollection<Photo> Photos { get; private set; } = new HashSet<Photo>();

        // Domain behavior
        public void Rename(string newName)
        {
            if (string.IsNullOrWhiteSpace(newName)) throw new ArgumentException("Name is required.", nameof(newName));
            Name = newName.Trim();
        }

        public void UpdateSpecs(string? brand, string? model)
        {
            Brand = (brand ?? string.Empty).Trim();
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

        public void Activate() => IsActive = true;
        public void Deactivate() => IsActive = false;

        public void AssignProperty(int propertyId)
        {
            if (propertyId <= 0) throw new ArgumentOutOfRangeException(nameof(propertyId));
            PropertyId = propertyId;
        }

        // Helper to construct a Point from lat/lng
        public static Point CreatePoint(double latitude, double longitude)
            => new Point(longitude, latitude) { SRID = 4326 };
    }
}