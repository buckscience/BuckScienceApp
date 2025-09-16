using BuckScience.Application.Analytics;

namespace BuckScience.Tests.Application.Analytics;

public class BuckLensAnalyticsServiceTests
{
    [Fact]
    public void SightingLocationAggregation_GroupsByCameraLocation()
    {
        // Arrange - create test sighting data with multiple sightings at same camera
        var sightings = new List<SightingData>
        {
            new SightingData
            {
                CameraId = 1,
                CameraName = "Trail Camera A",
                Latitude = 31.44147,
                Longitude = -93.438233,
                DateTaken = DateTime.Parse("2024-01-01 08:00"),
                Temperature = 45.5,
                WindDirectionText = "North",
                MoonPhaseText = "Full Moon"
            },
            new SightingData
            {
                CameraId = 1,
                CameraName = "Trail Camera A",
                Latitude = 31.44147,
                Longitude = -93.438233,
                DateTaken = DateTime.Parse("2024-01-02 09:00"),
                Temperature = 50.0,
                WindDirectionText = "North",
                MoonPhaseText = "Waning Gibbous"
            },
            new SightingData
            {
                CameraId = 1,
                CameraName = "Trail Camera A", 
                Latitude = 31.44147,
                Longitude = -93.438233,
                DateTaken = DateTime.Parse("2024-01-03 07:30"),
                Temperature = 42.0,
                WindDirectionText = "South",
                MoonPhaseText = "Waning Gibbous"
            },
            new SightingData
            {
                CameraId = 2,
                CameraName = "Trail Camera B",
                Latitude = 31.45000,
                Longitude = -93.440000,
                DateTaken = DateTime.Parse("2024-01-01 10:00"),
                Temperature = 48.0,
                WindDirectionText = "East",
                MoonPhaseText = "Full Moon"
            }
        };

        // Act - simulate the aggregation logic from the controller
        var aggregatedLocations = sightings
            .Where(s => s.Latitude.HasValue && s.Longitude.HasValue)
            .GroupBy(s => new { s.CameraId, s.CameraName, s.Latitude, s.Longitude })
            .Select(g => new
            {
                CameraId = g.Key.CameraId,
                CameraName = g.Key.CameraName,
                Latitude = g.Key.Latitude!.Value,
                Longitude = g.Key.Longitude!.Value,
                SightingCount = g.Count(),
                MostRecentSighting = g.OrderByDescending(s => s.DateTaken).First(),
                FirstSighting = g.Min(s => s.DateTaken),
                LastSighting = g.Max(s => s.DateTaken),
                AvgTemperature = g.Where(s => s.Temperature.HasValue).Select(s => s.Temperature!.Value).DefaultIfEmpty().Average(),
                CommonWindDirection = g.GroupBy(s => s.WindDirectionText)
                    .Where(grp => !string.IsNullOrEmpty(grp.Key))
                    .OrderByDescending(grp => grp.Count())
                    .FirstOrDefault()?.Key,
                CommonMoonPhase = g.GroupBy(s => s.MoonPhaseText)
                    .Where(grp => !string.IsNullOrEmpty(grp.Key))
                    .OrderByDescending(grp => grp.Count())
                    .FirstOrDefault()?.Key
            })
            .OrderByDescending(l => l.SightingCount)
            .ToList();

        // Assert - verify that sightings are properly aggregated by camera location
        Assert.Equal(2, aggregatedLocations.Count);

        // Camera A should have 3 sightings
        var cameraA = aggregatedLocations.First(c => c.CameraId == 1);
        Assert.Equal("Trail Camera A", cameraA.CameraName);
        Assert.Equal(3, cameraA.SightingCount);
        Assert.Equal(31.44147, cameraA.Latitude);
        Assert.Equal(-93.438233, cameraA.Longitude);
        Assert.Equal(DateTime.Parse("2024-01-03 07:30"), cameraA.MostRecentSighting.DateTaken);
        Assert.Equal(DateTime.Parse("2024-01-01 08:00"), cameraA.FirstSighting);
        Assert.Equal(DateTime.Parse("2024-01-03 07:30"), cameraA.LastSighting);
        Assert.Equal(45.83333333333333, cameraA.AvgTemperature, 5); // (45.5 + 50.0 + 42.0) / 3
        Assert.Equal("North", cameraA.CommonWindDirection); // North appears twice, South once
        Assert.Equal("Waning Gibbous", cameraA.CommonMoonPhase); // Waning Gibbous appears twice

        // Camera B should have 1 sighting
        var cameraB = aggregatedLocations.First(c => c.CameraId == 2);
        Assert.Equal("Trail Camera B", cameraB.CameraName);
        Assert.Equal(1, cameraB.SightingCount);
        Assert.Equal(31.45000, cameraB.Latitude);
        Assert.Equal(-93.440000, cameraB.Longitude);
        Assert.Equal(48.0, cameraB.AvgTemperature);
        Assert.Equal("East", cameraB.CommonWindDirection);
        Assert.Equal("Full Moon", cameraB.CommonMoonPhase);

        // Verify ordering by sighting count (descending)
        Assert.True(aggregatedLocations[0].SightingCount >= aggregatedLocations[1].SightingCount);
    }

    [Fact]
    public void SightingLocationAggregation_HandlesNullCoordinates()
    {
        // Arrange - create test data with some null coordinates
        var sightings = new List<SightingData>
        {
            new SightingData
            {
                CameraId = 1,
                CameraName = "Trail Camera A",
                Latitude = 31.44147,
                Longitude = -93.438233,
                DateTaken = DateTime.Parse("2024-01-01 08:00")
            },
            new SightingData
            {
                CameraId = 2,
                CameraName = "Trail Camera B",
                Latitude = null, // No coordinates
                Longitude = null,
                DateTaken = DateTime.Parse("2024-01-01 09:00")
            }
        };

        // Act - filter and aggregate
        var aggregatedLocations = sightings
            .Where(s => s.Latitude.HasValue && s.Longitude.HasValue)
            .GroupBy(s => new { s.CameraId, s.CameraName, s.Latitude, s.Longitude })
            .Select(g => new
            {
                CameraId = g.Key.CameraId,
                SightingCount = g.Count()
            })
            .ToList();

        // Assert - only the camera with coordinates should be included
        Assert.Single(aggregatedLocations);
        Assert.Equal(1, aggregatedLocations[0].CameraId);
        Assert.Equal(1, aggregatedLocations[0].SightingCount);
    }
}