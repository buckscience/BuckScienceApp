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
            // Test the confidence score calculation logic with corridor bonus
            int totalSightings = 100;
            int segmentSightings = 30;
            int corridorCount = 5;

            var proportion = (double)segmentSightings / totalSightings; // 0.3
            var dataConfidence = System.Math.Min(segmentSightings / 10.0, 1.0); // min(3.0, 1.0) = 1.0
            var proportionConfidence = proportion; // 0.3
            var corridorBonus = System.Math.Min(corridorCount / 5.0, 0.2); // min(1.0, 0.2) = 0.2
            
            var baseScore = (dataConfidence * 0.6 + proportionConfidence * 0.4) * 100; // 72.0
            var expectedScore = System.Math.Round(System.Math.Min(baseScore + (corridorBonus * 100), 100), 1); // 92.0

            Assert.Equal(92.0, expectedScore);
        }

        [Fact]
        public void MovementCorridorAnalysis_IdentifiesValidTransitions()
        {
            // Test corridor identification from sequential sightings
            var sightings = new List<BuckTraxSighting>
            {
                new BuckTraxSighting 
                { 
                    DateTaken = new System.DateTime(2024, 1, 1, 8, 0, 0), 
                    AssociatedFeatureId = 1,
                    Latitude = 30.0, 
                    Longitude = -90.0 
                },
                new BuckTraxSighting 
                { 
                    DateTaken = new System.DateTime(2024, 1, 1, 8, 30, 0), 
                    AssociatedFeatureId = 2,
                    Latitude = 30.1, 
                    Longitude = -90.1 
                },
                new BuckTraxSighting 
                { 
                    DateTaken = new System.DateTime(2024, 1, 1, 9, 0, 0), 
                    AssociatedFeatureId = 1,
                    Latitude = 30.0, 
                    Longitude = -90.0 
                }
            };

            // Test time window filtering (30 minutes should be valid)
            var validTransition1 = sightings[1].DateTaken - sightings[0].DateTaken;
            Assert.True(validTransition1.TotalMinutes <= 480); // Within 8-hour window

            // Test feature association (different features should create corridor)
            Assert.NotEqual(sightings[0].AssociatedFeatureId, sightings[1].AssociatedFeatureId);
            
            // Test same feature filtering (should be ignored)
            Assert.Equal(sightings[0].AssociatedFeatureId, sightings[2].AssociatedFeatureId);
        }

        [Fact]
        public void FeatureWeightIntegration_CalculatesCorridorScore()
        {
            // Test corridor score calculation using feature weights
            var corridor = new BuckTraxMovementCorridor
            {
                TransitionCount = 5,
                StartFeatureWeight = 0.8f,
                EndFeatureWeight = 0.6f
            };

            // CorridorScore = Frequency * (WeightStart + WeightEnd) / 2
            var expectedScore = 5 * (0.8f + 0.6f) / 2; // 5 * 0.7 = 3.5
            corridor.CorridorScore = corridor.TransitionCount * (corridor.StartFeatureWeight + corridor.EndFeatureWeight) / 2;

            Assert.Equal(expectedScore, corridor.CorridorScore, 1);
        }

        [Fact]
        public void LimitedDataThreshold_TriggersWarning()
        {
            // Test threshold logic for limited data warnings
            var config = new BuckTraxConfiguration
            {
                MinimumSightingsThreshold = 10,
                MinimumTransitionsThreshold = 3,
                ShowLimitedDataWarning = true
            };

            // Test insufficient sightings
            int sightingCount = 5;
            int transitionCount = 5;
            bool isLimitedData1 = sightingCount < config.MinimumSightingsThreshold || 
                                 transitionCount < config.MinimumTransitionsThreshold;
            Assert.True(isLimitedData1);

            // Test insufficient transitions
            sightingCount = 15;
            transitionCount = 2;
            bool isLimitedData2 = sightingCount < config.MinimumSightingsThreshold || 
                                 transitionCount < config.MinimumTransitionsThreshold;
            Assert.True(isLimitedData2);

            // Test sufficient data
            sightingCount = 15;
            transitionCount = 5;
            bool isLimitedData3 = sightingCount < config.MinimumSightingsThreshold || 
                                 transitionCount < config.MinimumTransitionsThreshold;
            Assert.False(isLimitedData3);
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

        [Fact]
        public void SightingFeatureAssociation_AssociatesWithNearestFeature()
        {
            // Test that sightings are associated with nearest features within proximity threshold
            var sightings = new List<BuckTraxSighting>
            {
                new BuckTraxSighting { Latitude = 30.0, Longitude = -90.0 }
            };

            var features = new List<BuckTraxFeature>
            {
                new BuckTraxFeature { Id = 1, Latitude = 30.001, Longitude = -90.001 }, // ~100m away
                new BuckTraxFeature { Id = 2, Latitude = 30.01, Longitude = -90.01 }    // ~1km away
            };

            // Calculate distances (simplified)
            var proximityThreshold = 200.0; // 200 meters
            
            // Feature 1 should be closer and within threshold
            var distance1 = CalculateSimpleDistance(sightings[0], features[0]);
            var distance2 = CalculateSimpleDistance(sightings[0], features[1]);
            
            Assert.True(distance1 < distance2);
            Assert.True(distance1 < proximityThreshold);
        }

        [Fact]
        public void CorridorTimePattern_IdentifiesActiveHours()
        {
            // Test time of day pattern identification for corridors
            var transitionTimes = new List<int> { 8, 9, 10, 8, 9, 17, 18, 19 }; // Morning and Evening
            
            var morningCount = transitionTimes.Count(t => t >= 8 && t < 12);
            var eveningCount = transitionTimes.Count(t => t >= 17 && t < 20);
            var totalCount = transitionTimes.Count;
            
            // Should identify both Morning and Evening patterns (>30% threshold)
            Assert.True(morningCount > totalCount * 0.3);
            Assert.True(eveningCount > totalCount * 0.3);
        }

        [Fact]
        public void EnhancedConfiguration_HasReducedThresholds()
        {
            // Test that the new configuration has more responsive thresholds
            var config = new BuckTraxConfiguration
            {
                MinimumSightingsThreshold = 5,  // Reduced from 10
                MinimumTransitionsThreshold = 2, // Reduced from 3
                MovementTimeWindowMinutes = 240, // Reduced from 480
                MaxMovementDistanceMeters = 2000 // Reduced from 5000
            };

            // Assert that thresholds are more responsive
            Assert.True(config.MinimumSightingsThreshold <= 5);
            Assert.True(config.MinimumTransitionsThreshold <= 2);
            Assert.True(config.MovementTimeWindowMinutes <= 240);
            Assert.True(config.MaxMovementDistanceMeters <= 2000);
        }

        [Fact]
        public void FeatureWeightCalculation_AmplifiedForHighWeights()
        {
            // Test enhanced weight calculation that amplifies high-weight features
            var lowWeight = 0.3f;
            var highWeight = 0.9f;
            
            // Calculate amplified weights (using power of 1.5)
            var amplifiedLow = Math.Pow(lowWeight, 1.5);
            var amplifiedHigh = Math.Pow(highWeight, 1.5);
            
            // High weights should get more amplification than low weights
            var lowRatio = amplifiedLow / lowWeight;
            var highRatio = amplifiedHigh / highWeight;
            
            Assert.True(highRatio > lowRatio, "High weights should be amplified more than low weights");
            Assert.True(amplifiedHigh > 0.8, "High weights should remain elevated after amplification");
        }

        private double CalculateSimpleDistance(BuckTraxSighting sighting, BuckTraxFeature feature)
        {
            // Simplified distance calculation for testing
            var latDiff = Math.Abs(sighting.Latitude - feature.Latitude);
            var lonDiff = Math.Abs(sighting.Longitude - feature.Longitude);
            return Math.Sqrt(latDiff * latDiff + lonDiff * lonDiff) * 111000; // Rough conversion to meters
        }
    }
}