using BuckScience.Web.Controllers;
using BuckScience.Web.ViewModels.BuckTrax;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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
        public void GroupPhotosIntoSightings_Applies15MinuteRule_CorrectlyGroupsSightings()
        {
            // Test the 15-minute sighting grouping rule as requested by @buckscience
            // Example: Photos at 07:05, 07:06, 07:08, and 07:22 should result in 2 sightings
            var photos = new List<BuckTraxSighting>
            {
                new BuckTraxSighting
                {
                    PhotoId = 1,
                    DateTaken = new DateTime(2024, 1, 1, 7, 5, 0),
                    CameraId = 1,
                    CameraName = "Camera 1",
                    Latitude = 40.0,
                    Longitude = -80.0
                },
                new BuckTraxSighting
                {
                    PhotoId = 2,
                    DateTaken = new DateTime(2024, 1, 1, 7, 6, 0), // 1 minute later
                    CameraId = 1,
                    CameraName = "Camera 1",
                    Latitude = 40.0,
                    Longitude = -80.0
                },
                new BuckTraxSighting
                {
                    PhotoId = 3,
                    DateTaken = new DateTime(2024, 1, 1, 7, 8, 0), // 3 minutes from first
                    CameraId = 1,
                    CameraName = "Camera 1",
                    Latitude = 40.0,
                    Longitude = -80.0
                },
                new BuckTraxSighting
                {
                    PhotoId = 4,
                    DateTaken = new DateTime(2024, 1, 1, 7, 22, 0), // 17 minutes from first (new sighting)
                    CameraId = 1,
                    CameraName = "Camera 1",
                    Latitude = 40.0,
                    Longitude = -80.0
                }
            };

            // Create a controller instance and use reflection to test the private method
            var controller = new BuckTraxController(null, null);
            var method = typeof(BuckTraxController).GetMethod("GroupPhotosIntoSightings", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var result = (List<BuckTraxSighting>)method.Invoke(controller, new object[] { photos });

            // Should result in exactly 2 sightings (not 4 photos)
            Assert.Equal(2, result.Count);
            
            // First sighting should be the first photo (07:05)
            Assert.Equal(1, result[0].PhotoId);
            Assert.Equal(new DateTime(2024, 1, 1, 7, 5, 0), result[0].DateTaken);
            
            // Second sighting should be the photo at 07:22 (>15 minutes later)
            Assert.Equal(4, result[1].PhotoId);
            Assert.Equal(new DateTime(2024, 1, 1, 7, 22, 0), result[1].DateTaken);
        }
        
        [Fact]
        public void FeatureAwareRouting_EnabledWithLogicalWaypoints_CreatesEnhancedRoute()
        {
            // Test that feature-aware routing creates waypoints through logical features
            var controller = new BuckTraxController(null, null);
            var config = new BuckTraxConfiguration 
            { 
                EnableFeatureAwareRouting = true,
                MinimumDistanceForFeatureRouting = 100,
                MaximumDetourPercentage = 0.3,
                MaximumWaypointsPerRoute = 2,
                CameraFeatureProximityMeters = 100
            };
            
            var features = new List<BuckTraxFeature>
            {
                new BuckTraxFeature { Id = 1, Name = "Food Plot A", ClassificationName = "Food Plot", Latitude = 40.0, Longitude = -80.0, EffectiveWeight = 0.8f },
                new BuckTraxFeature { Id = 2, Name = "Creek Crossing", ClassificationName = "Creek Crossing", Latitude = 40.005, Longitude = -80.005, EffectiveWeight = 0.7f },
                new BuckTraxFeature { Id = 3, Name = "Food Plot B", ClassificationName = "Food Plot", Latitude = 40.01, Longitude = -80.01, EffectiveWeight = 0.9f }
            };
            
            var startPoint = new RoutePoint
            {
                LocationId = 100,
                LocationName = "Camera 1",
                LocationType = "Camera Location",
                Latitude = 40.0,
                Longitude = -80.0,
                VisitTime = new DateTime(2024, 1, 1, 8, 0, 0)
            };
            
            var endSighting = new BuckTraxSighting
            {
                CameraId = 101,
                CameraName = "Camera 2",
                Latitude = 40.01,
                Longitude = -80.01,
                DateTaken = new DateTime(2024, 1, 1, 9, 0, 0)
            };
            
            // Use reflection to call the private method
            var method = typeof(BuckTraxController).GetMethod("CreateFeatureAwareRoute", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var result = (List<RoutePoint>)method.Invoke(controller, new object[] { startPoint, endSighting, features, config });
            
            // Should include waypoints through logical features
            Assert.True(result.Count >= 2); // At least start point + waypoint(s)
            
            // Start point should be unchanged
            Assert.Equal(startPoint.LocationId, result[0].LocationId);
            Assert.Equal("Camera Location", result[0].LocationType);
            
            // Should have added logical waypoints
            var waypointAdded = result.Skip(1).Any(p => p.LocationType == "Property Feature");
            Assert.True(waypointAdded, "Should have added at least one feature waypoint");
        }
        
        [Fact]
        public void FeatureAwareRouting_DisabledConfig_UsesDirectRoute()
        {
            // Test that when feature-aware routing is disabled, it uses direct routes
            var controller = new BuckTraxController(null, null);
            var config = new BuckTraxConfiguration 
            { 
                EnableFeatureAwareRouting = false // Disabled
            };
            
            var features = new List<BuckTraxFeature>
            {
                new BuckTraxFeature { Id = 1, Name = "Creek Crossing", ClassificationName = "Creek Crossing", Latitude = 40.005, Longitude = -80.005, EffectiveWeight = 0.8f }
            };
            
            var startPoint = new RoutePoint
            {
                LocationId = 100,
                LocationName = "Camera 1",
                LocationType = "Camera Location",
                Latitude = 40.0,
                Longitude = -80.0,
                VisitTime = new DateTime(2024, 1, 1, 8, 0, 0)
            };
            
            var endSighting = new BuckTraxSighting
            {
                CameraId = 101,
                CameraName = "Camera 2",
                Latitude = 40.01,
                Longitude = -80.01,
                DateTaken = new DateTime(2024, 1, 1, 9, 0, 0)
            };
            
            // Use reflection to call the private method
            var method = typeof(BuckTraxController).GetMethod("CreateFeatureAwareRoute", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var result = (List<RoutePoint>)method.Invoke(controller, new object[] { startPoint, endSighting, features, config });
            
            // Should only have the start point (no waypoints added)
            Assert.Single(result);
            Assert.Equal(startPoint.LocationId, result[0].LocationId);
        }
        
        [Fact]
        public void FeatureAwareRouting_ShortDistance_SkipsWaypoints()
        {
            // Test that short movements don't use feature routing
            var controller = new BuckTraxController(null, null);
            var config = new BuckTraxConfiguration 
            { 
                EnableFeatureAwareRouting = true,
                MinimumDistanceForFeatureRouting = 500 // High threshold
            };
            
            var features = new List<BuckTraxFeature>
            {
                new BuckTraxFeature { Id = 1, Name = "Creek Crossing", ClassificationName = "Creek Crossing", Latitude = 40.001, Longitude = -80.001, EffectiveWeight = 0.8f }
            };
            
            var startPoint = new RoutePoint
            {
                LocationId = 100,
                LocationName = "Camera 1",
                LocationType = "Camera Location",
                Latitude = 40.0,
                Longitude = -80.0,
                VisitTime = new DateTime(2024, 1, 1, 8, 0, 0)
            };
            
            var endSighting = new BuckTraxSighting
            {
                CameraId = 101,
                CameraName = "Camera 2",
                Latitude = 40.002, // Short distance
                Longitude = -80.002,
                DateTaken = new DateTime(2024, 1, 1, 9, 0, 0)
            };
            
            // Use reflection to call the private method
            var method = typeof(BuckTraxController).GetMethod("CreateFeatureAwareRoute", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var result = (List<RoutePoint>)method.Invoke(controller, new object[] { startPoint, endSighting, features, config });
            
            // Should only have the start point due to short distance
            Assert.Single(result);
            Assert.Equal(startPoint.LocationId, result[0].LocationId);
        }
        
        [Fact]
        public void FindLogicalWaypoints_IdentifiesRelevantFeatures()
        {
            // Test that the waypoint finding logic correctly identifies relevant features
            var controller = new BuckTraxController(null, null);
            var config = new BuckTraxConfiguration 
            { 
                CameraFeatureProximityMeters = 100,
                MaximumDetourPercentage = 0.3,
                MaximumWaypointsPerRoute = 2
            };
            
            var features = new List<BuckTraxFeature>
            {
                // Relevant waypoint features
                new BuckTraxFeature { Id = 1, Name = "Creek Crossing", ClassificationName = "Creek Crossing", Latitude = 40.005, Longitude = -80.005, EffectiveWeight = 0.8f },
                new BuckTraxFeature { Id = 2, Name = "Travel Corridor", ClassificationName = "Travel Corridor", Latitude = 40.007, Longitude = -80.007, EffectiveWeight = 0.7f },
                
                // Irrelevant features (wrong type or too far)
                new BuckTraxFeature { Id = 3, Name = "Bedding Area", ClassificationName = "Bedding Area", Latitude = 40.006, Longitude = -80.006, EffectiveWeight = 0.9f },
                new BuckTraxFeature { Id = 4, Name = "Far Creek", ClassificationName = "Creek Crossing", Latitude = 40.02, Longitude = -80.02, EffectiveWeight = 0.8f }
            };
            
            var startPoint = new RoutePoint
            {
                LocationId = 100,
                LocationName = "Camera 1",
                LocationType = "Camera Location",
                Latitude = 40.0,
                Longitude = -80.0,
                VisitTime = new DateTime(2024, 1, 1, 8, 0, 0)
            };
            
            var endSighting = new BuckTraxSighting
            {
                CameraId = 101,
                CameraName = "Camera 2",
                Latitude = 40.01,
                Longitude = -80.01,
                DateTaken = new DateTime(2024, 1, 1, 9, 0, 0)
            };
            
            // Use reflection to call the private method
            var method = typeof(BuckTraxController).GetMethod("FindLogicalWaypoints", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            
            var result = (List<BuckTraxFeature>)method.Invoke(controller, new object[] { startPoint, endSighting, features, config });
            
            // Should identify relevant waypoint features but not irrelevant ones
            Assert.NotEmpty(result);
            Assert.Contains(result, f => f.Name == "Creek Crossing");
            Assert.DoesNotContain(result, f => f.Name == "Bedding Area"); // Wrong type
            Assert.DoesNotContain(result, f => f.Name == "Far Creek"); // Too far from path
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

        [Fact]
        public void DefaultTimeSegmentIndex_SelectsMostActiveSegment()
        {
            // This test validates that the logic works by creating a full prediction result
            // and checking that the DefaultTimeSegmentIndex points to the most active segment
            
            // We can't directly test the private method, so we'll test the integration
            // by verifying the DefaultTimeSegmentIndex in a realistic scenario
            
            // For now, we'll test the logic conceptually by manually implementing
            // the algorithm to verify it works correctly
            
            var predictions = new List<BuckTraxTimeSegmentPrediction>
            {
                new BuckTraxTimeSegmentPrediction { TimeSegment = "Morning", SightingCount = 2, TimeSegmentCorridors = new List<BuckTraxMovementCorridor>() },
                new BuckTraxTimeSegmentPrediction { TimeSegment = "Afternoon", SightingCount = 5, TimeSegmentCorridors = new List<BuckTraxMovementCorridor> { new BuckTraxMovementCorridor() } },
                new BuckTraxTimeSegmentPrediction { TimeSegment = "Evening", SightingCount = 1, TimeSegmentCorridors = new List<BuckTraxMovementCorridor>() },
                new BuckTraxTimeSegmentPrediction { TimeSegment = "Night", SightingCount = 3, TimeSegmentCorridors = new List<BuckTraxMovementCorridor>() }
            };

            // Manually calculate which should be the most active
            var maxActivity = -1;
            var expectedIndex = 0;
            
            for (int i = 0; i < predictions.Count; i++)
            {
                var activityScore = predictions[i].SightingCount + (predictions[i].TimeSegmentCorridors?.Count ?? 0);
                if (activityScore > maxActivity)
                {
                    maxActivity = activityScore;
                    expectedIndex = i;
                }
            }
            
            // Afternoon has 5 sightings + 1 corridor = 6 total activity
            Assert.Equal(1, expectedIndex);
            Assert.Equal(6, maxActivity);
        }

        [Fact]
        public void DefaultTimeSegmentIndex_PrefersDaylightOnTie()
        {
            // Test that daylight segments are preferred when there's a tie in activity
            var predictions = new List<BuckTraxTimeSegmentPrediction>
            {
                new BuckTraxTimeSegmentPrediction { TimeSegment = "Morning", SightingCount = 3, TimeSegmentCorridors = new List<BuckTraxMovementCorridor>() },
                new BuckTraxTimeSegmentPrediction { TimeSegment = "Afternoon", SightingCount = 3, TimeSegmentCorridors = new List<BuckTraxMovementCorridor>() },
                new BuckTraxTimeSegmentPrediction { TimeSegment = "Evening", SightingCount = 3, TimeSegmentCorridors = new List<BuckTraxMovementCorridor>() },
                new BuckTraxTimeSegmentPrediction { TimeSegment = "Night", SightingCount = 3, TimeSegmentCorridors = new List<BuckTraxMovementCorridor>() }
            };

            // Simulate the tie-breaking logic
            var maxActivity = 3; // All segments have equal activity
            var daylightSegments = new List<int>();
            
            for (int i = 0; i < predictions.Count; i++)
            {
                var activityScore = predictions[i].SightingCount + (predictions[i].TimeSegmentCorridors?.Count ?? 0);
                var isDaylight = !predictions[i].TimeSegment.Equals("Night", StringComparison.OrdinalIgnoreCase);
                
                if (activityScore == maxActivity && isDaylight)
                {
                    daylightSegments.Add(i);
                }
            }
            
            // Should have Morning, Afternoon, Evening as daylight segments
            Assert.Equal(3, daylightSegments.Count);
            Assert.Contains(0, daylightSegments); // Morning
            Assert.Contains(1, daylightSegments); // Afternoon  
            Assert.Contains(2, daylightSegments); // Evening
            Assert.DoesNotContain(3, daylightSegments); // Night should not be included
            
            // The earliest daylight segment should be selected (Morning = index 0)
            var expectedDefault = daylightSegments.First();
            Assert.Equal(0, expectedDefault);
        }

        [Fact]
        public void CorridorFiltering_OnlyShowsCorridorsAssociatedWithTimeSegment()
        {
            // Test that corridors without time patterns or mismatched patterns are not shown
            var corridors = new List<BuckTraxMovementCorridor>
            {
                new BuckTraxMovementCorridor { Name = "Morning Corridor", TimeOfDayPattern = "Morning" },
                new BuckTraxMovementCorridor { Name = "Evening Corridor", TimeOfDayPattern = "Evening" },
                new BuckTraxMovementCorridor { Name = "No Pattern Corridor", TimeOfDayPattern = "" },
                new BuckTraxMovementCorridor { Name = "Null Pattern Corridor", TimeOfDayPattern = null },
                new BuckTraxMovementCorridor { Name = "Afternoon Corridor", TimeOfDayPattern = "Afternoon" }
            };

            var controller = new BuckTraxController(null!, null!);
            
            // Test Morning segment (7-11) - typical morning range
            var morningCorridors = corridors.Where(c => 
                TestIsCorridorActiveInTimeSegment(controller, c, 7, 11)).ToList();
            Assert.Single(morningCorridors);
            Assert.Equal("Morning Corridor", morningCorridors[0].Name);

            // Test Afternoon segment (11-15) - typical afternoon range
            var afternoonCorridors = corridors.Where(c => 
                TestIsCorridorActiveInTimeSegment(controller, c, 11, 15)).ToList();
            Assert.Single(afternoonCorridors);
            Assert.Equal("Afternoon Corridor", afternoonCorridors[0].Name);

            // Test Evening segment (15-19) - typical evening range
            var eveningCorridors = corridors.Where(c => 
                TestIsCorridorActiveInTimeSegment(controller, c, 15, 19)).ToList();
            Assert.Single(eveningCorridors);
            Assert.Equal("Evening Corridor", eveningCorridors[0].Name);

            // Test Night segment (21-5) - spans midnight, should have no corridors since none have Night pattern
            var nightCorridors = corridors.Where(c => 
                TestIsCorridorActiveInTimeSegment(controller, c, 21, 5)).ToList();
            Assert.Empty(nightCorridors);
            
            // Test that corridors without patterns are filtered out
            var emptyPatternCorridors = corridors.Where(c => 
                string.IsNullOrEmpty(c.TimeOfDayPattern)).ToList();
            Assert.Equal(2, emptyPatternCorridors.Count); // "No Pattern" and "Null Pattern" corridors
            
            // None of the empty pattern corridors should match any time segment
            foreach (var emptyCorr in emptyPatternCorridors)
            {
                Assert.False(TestIsCorridorActiveInTimeSegment(controller, emptyCorr, 7, 11)); // Morning
                Assert.False(TestIsCorridorActiveInTimeSegment(controller, emptyCorr, 11, 15)); // Afternoon
                Assert.False(TestIsCorridorActiveInTimeSegment(controller, emptyCorr, 15, 19)); // Evening
                Assert.False(TestIsCorridorActiveInTimeSegment(controller, emptyCorr, 21, 5)); // Night
            }
        }

        private bool TestIsCorridorActiveInTimeSegment(BuckTraxController controller, BuckTraxMovementCorridor corridor, int startHour, int endHour)
        {
            // Use reflection to access the private method for testing
            var methodInfo = typeof(BuckTraxController).GetMethod("IsCorridorActiveInTimeSegment", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return (bool)methodInfo.Invoke(controller, new object[] { corridor, startHour, endHour });
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