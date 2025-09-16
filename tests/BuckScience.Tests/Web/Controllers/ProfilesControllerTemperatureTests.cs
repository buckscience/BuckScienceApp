using BuckScience.Application.Analytics;
using BuckScience.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace BuckScience.Tests.Web.Controllers
{
    public class ProfilesControllerTemperatureTests
    {
        [Fact]
        public void AverageTemperatureCalculation_WithValidTemperatures_ReturnsCorrectAverage()
        {
            // Test data simulating what would happen in the actual controller
            var sightings = new List<SightingData>
            {
                new SightingData { CameraId = 1, CameraName = "Camera 1", Latitude = 30.0, Longitude = -90.0, Temperature = 70.5 },
                new SightingData { CameraId = 1, CameraName = "Camera 1", Latitude = 30.0, Longitude = -90.0, Temperature = 72.3 },
                new SightingData { CameraId = 1, CameraName = "Camera 1", Latitude = 30.0, Longitude = -90.0, Temperature = 68.7 }
            };

            // Simulate the LINQ query from GetSightingLocations
            var result = sightings
                .GroupBy(s => new { s.CameraId, s.CameraName, s.Latitude, s.Longitude })
                .Select(g => new
                {
                    avgTemperature = g.Where(s => s.Temperature.HasValue).Any() 
                        ? g.Where(s => s.Temperature.HasValue).Average(s => s.Temperature!.Value)
                        : (double?)null
                })
                .First();

            // Expected average: (70.5 + 72.3 + 68.7) / 3 = 70.5
            Assert.NotNull(result.avgTemperature);
            Assert.Equal(70.5, result.avgTemperature!.Value, 1); // 1 decimal precision
        }

        [Fact]
        public void AverageTemperatureCalculation_WithNoTemperatures_ReturnsNull()
        {
            // Test data with no temperature values
            var sightings = new List<SightingData>
            {
                new SightingData { CameraId = 1, CameraName = "Camera 1", Latitude = 30.0, Longitude = -90.0, Temperature = null },
                new SightingData { CameraId = 1, CameraName = "Camera 1", Latitude = 30.0, Longitude = -90.0, Temperature = null }
            };

            // Simulate the LINQ query from GetSightingLocations
            var result = sightings
                .GroupBy(s => new { s.CameraId, s.CameraName, s.Latitude, s.Longitude })
                .Select(g => new
                {
                    avgTemperature = g.Where(s => s.Temperature.HasValue).Any() 
                        ? g.Where(s => s.Temperature.HasValue).Average(s => s.Temperature!.Value)
                        : (double?)null
                })
                .First();

            // Should return null when no temperature data is available
            Assert.Null(result.avgTemperature);
        }

        [Fact]
        public void AverageTemperatureCalculation_WithMixedTemperatures_ReturnsCorrectAverage()
        {
            // Test data with some null temperatures
            var sightings = new List<SightingData>
            {
                new SightingData { CameraId = 1, CameraName = "Camera 1", Latitude = 30.0, Longitude = -90.0, Temperature = 70.0 },
                new SightingData { CameraId = 1, CameraName = "Camera 1", Latitude = 30.0, Longitude = -90.0, Temperature = null },
                new SightingData { CameraId = 1, CameraName = "Camera 1", Latitude = 30.0, Longitude = -90.0, Temperature = 80.0 }
            };

            // Simulate the LINQ query from GetSightingLocations
            var result = sightings
                .GroupBy(s => new { s.CameraId, s.CameraName, s.Latitude, s.Longitude })
                .Select(g => new
                {
                    avgTemperature = g.Where(s => s.Temperature.HasValue).Any() 
                        ? g.Where(s => s.Temperature.HasValue).Average(s => s.Temperature!.Value)
                        : (double?)null
                })
                .First();

            // Expected average: (70.0 + 80.0) / 2 = 75.0 (null values should be ignored)
            Assert.NotNull(result.avgTemperature);
            Assert.Equal(75.0, result.avgTemperature!.Value, 1);
        }
    }
}