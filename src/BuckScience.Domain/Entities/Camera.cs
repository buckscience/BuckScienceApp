using System;
using System.Collections.Generic;
using NetTopologySuite.Geometries;

namespace BuckScience.Domain.Entities
{
    public class Camera
    {
        public int Id { get; set; }

        public string Name { get; set; } = default!;
        public string Brand { get; set; } = string.Empty;
        public string? Model { get; set; }

        // Spatial point (WGS84). X = Longitude, Y = Latitude.
        public Point Location { get; set; } = default!;

        // Convenience accessors (NTS uses X=Longitude, Y=Latitude)
        public double Latitude => Location?.Y ?? 0d;
        public double Longitude => Location?.X ?? 0d;

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public bool IsActive { get; set; } = true;

        // Domain-only convenience (not persisted)
        public int PhotoCount { get; set; }

        // FK + navigation
        public int PropertyId { get; set; }
        public virtual Property Property { get; set; } = default!;

        public virtual ICollection<Photo> Photos { get; set; } = new List<Photo>();

        // Helper to set location from lat/lng
        public static Point CreatePoint(double latitude, double longitude)
            => new Point(longitude, latitude) { SRID = 4326 };
    }
}