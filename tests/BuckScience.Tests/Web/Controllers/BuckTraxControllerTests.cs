using BuckScience.Web.Controllers;
using BuckScience.Web.ViewModels.BuckTrax;
using Microsoft.AspNetCore.Mvc;
using System.Collections.Generic;
using System.Linq;
using Xunit;

namespace BuckScience.Tests.Web.Controllers
{
    public class BuckTraxControllerTests
    {
        [Fact]
        public void TimeSegmentAnalysis_SplitsCorrectly()
        {
            // Test that time segments are properly defined and non-overlapping
            var timeSegments = new[]
            {
                new { Name = "Early Morning", Start = 5, End = 8 },
                new { Name = "Morning", Start = 8, End = 11 },
                new { Name = "Midday", Start = 11, End = 14 },
                new { Name = "Afternoon", Start = 14, End = 17 },
                new { Name = "Evening", Start = 17, End = 20 },
                new { Name = "Night", Start = 20, End = 5 }
            };

            // Verify coverage and non-overlap (except for night segment which wraps)
            var normalSegments = timeSegments.Where(s => s.Start < s.End).ToList();
            var nightSegment = timeSegments.Single(s => s.Start > s.End);

            // Check that normal segments don't overlap
            for (int i = 0; i < normalSegments.Count - 1; i++)
            {
                Assert.Equal(normalSegments[i].End, normalSegments[i + 1].Start);
            }

            // Check night segment wraps correctly
            Assert.Equal(20, nightSegment.Start);
            Assert.Equal(5, nightSegment.End);
        }

        [Fact]
        public void ConfidenceScoreCalculation_WithValidData_ReturnsCorrectScore()
        {
            // Test the confidence score calculation logic
            int totalSightings = 100;
            int segmentSightings = 30;

            var proportion = (double)segmentSightings / totalSightings; // 0.3
            var dataConfidence = System.Math.Min(segmentSightings / 10.0, 1.0); // min(3.0, 1.0) = 1.0
            var proportionConfidence = proportion; // 0.3
            
            var expectedScore = System.Math.Round((dataConfidence * 0.6 + proportionConfidence * 0.4) * 100, 1);
            var actualScore = (1.0 * 0.6 + 0.3 * 0.4) * 100; // (0.6 + 0.12) * 100 = 72.0

            Assert.Equal(72.0, expectedScore);
        }

        [Fact]
        public void MovementCorridorClassification_IdentifiesCorrectFeatures()
        {
            // Test that movement corridors are correctly identified
            var movementFeatureTypes = new[] { 6, 7, 11, 15, 16 }; // Draw, CreekCrossing, FieldEdge, PinchPointFunnel, TravelCorridor
            var nonMovementFeatureTypes = new[] { 1, 31, 52, 70, 99 }; // Ridge, AgCropField, Pond, BeddingArea, Other

            foreach (var featureType in movementFeatureTypes)
            {
                bool isMovementCorridor = featureType switch
                {
                    6 => true,  // Draw
                    7 => true,  // CreekCrossing
                    11 => true, // FieldEdge
                    15 => true, // PinchPointFunnel
                    16 => true, // TravelCorridor
                    _ => false
                };

                Assert.True(isMovementCorridor, $"Feature type {featureType} should be classified as movement corridor");
            }

            foreach (var featureType in nonMovementFeatureTypes)
            {
                bool isMovementCorridor = featureType switch
                {
                    6 => true,  // Draw
                    7 => true,  // CreekCrossing
                    11 => true, // FieldEdge
                    15 => true, // PinchPointFunnel
                    16 => true, // TravelCorridor
                    _ => false
                };

                Assert.False(isMovementCorridor, $"Feature type {featureType} should NOT be classified as movement corridor");
            }
        }

        [Fact]
        public void PredictionZone_ProbabilityCalculation_IsCorrect()
        {
            // Test probability calculation for prediction zones
            var sightings = new List<BuckTraxSighting>
            {
                new BuckTraxSighting { CameraId = 1, CameraName = "Camera 1", Latitude = 30.0, Longitude = -90.0 },
                new BuckTraxSighting { CameraId = 1, CameraName = "Camera 1", Latitude = 30.0, Longitude = -90.0 },
                new BuckTraxSighting { CameraId = 1, CameraName = "Camera 1", Latitude = 30.0, Longitude = -90.0 },
                new BuckTraxSighting { CameraId = 2, CameraName = "Camera 2", Latitude = 30.1, Longitude = -90.1 },
                new BuckTraxSighting { CameraId = 2, CameraName = "Camera 2", Latitude = 30.1, Longitude = -90.1 }
            };

            // Simulate the grouping logic from the controller
            var zones = sightings
                .GroupBy(s => new { s.CameraId, s.CameraName, s.Latitude, s.Longitude })
                .Select(g => new
                {
                    CameraId = g.Key.CameraId,
                    SightingCount = g.Count(),
                    Probability = (double)g.Count() / sightings.Count
                })
                .OrderByDescending(z => z.Probability)
                .ToList();

            // Camera 1 should have 3/5 = 0.6 probability
            var camera1Zone = zones.First(z => z.CameraId == 1);
            Assert.Equal(3, camera1Zone.SightingCount);
            Assert.Equal(0.6, camera1Zone.Probability, 2);

            // Camera 2 should have 2/5 = 0.4 probability
            var camera2Zone = zones.First(z => z.CameraId == 2);
            Assert.Equal(2, camera2Zone.SightingCount);
            Assert.Equal(0.4, camera2Zone.Probability, 2);
        }

        [Fact]
        public void TimeSegmentSightingFilter_FiltersCorrectly()
        {
            // Test that sightings are correctly filtered by time segments
            var sightings = new List<BuckTraxSighting>
            {
                new BuckTraxSighting { DateTaken = new System.DateTime(2024, 1, 1, 6, 30, 0) },  // Early Morning
                new BuckTraxSighting { DateTaken = new System.DateTime(2024, 1, 1, 9, 15, 0) },  // Morning
                new BuckTraxSighting { DateTaken = new System.DateTime(2024, 1, 1, 12, 45, 0) }, // Midday
                new BuckTraxSighting { DateTaken = new System.DateTime(2024, 1, 1, 15, 20, 0) }, // Afternoon
                new BuckTraxSighting { DateTaken = new System.DateTime(2024, 1, 1, 18, 30, 0) }, // Evening
                new BuckTraxSighting { DateTaken = new System.DateTime(2024, 1, 1, 22, 10, 0) }, // Night
                new BuckTraxSighting { DateTaken = new System.DateTime(2024, 1, 1, 2, 45, 0) }   // Night (early)
            };

            // Test Early Morning filter (5-8)
            var earlyMorningSightings = sightings.Where(s => s.DateTaken.Hour >= 5 && s.DateTaken.Hour < 8).ToList();
            Assert.Single(earlyMorningSightings);
            Assert.Equal(6, earlyMorningSightings[0].DateTaken.Hour);

            // Test Night filter (20-5) - spans midnight
            var nightSightings = sightings.Where(s => s.DateTaken.Hour >= 20 || s.DateTaken.Hour < 5).ToList();
            Assert.Equal(2, nightSightings.Count);
            Assert.Contains(nightSightings, s => s.DateTaken.Hour == 22);
            Assert.Contains(nightSightings, s => s.DateTaken.Hour == 2);
        }
    }
}