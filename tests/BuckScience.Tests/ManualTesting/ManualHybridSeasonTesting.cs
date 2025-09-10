using BuckScience.Application.FeatureWeights;
using BuckScience.Application.Services;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using BuckScience.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Tests.ManualTesting
{
    /// <summary>
    /// Manual testing scenarios for hybrid season mapping in feature weight retrieval.
    /// These tests can be run individually to verify real-world scenarios.
    /// </summary>
    public class ManualHybridSeasonTesting
    {
        public static async Task RunAllScenariosAsync()
        {
            Console.WriteLine("=== Manual Hybrid Season Mapping Testing ===\n");

            using var dbContext = CreateInMemoryDbContext();
            var seasonService = new SeasonMonthMappingService(dbContext);

            await TestHunterScenarioAsync(dbContext, seasonService);
            await TestLandManagerScenarioAsync(dbContext, seasonService);
            await TestResearcherScenarioAsync(dbContext, seasonService);
            await TestEdgeCasesAsync(dbContext, seasonService);

            Console.WriteLine("=== All Manual Tests Completed ===");
        }

        /// <summary>
        /// Test scenario: Recreational hunter using default season mappings
        /// </summary>
        private static async Task TestHunterScenarioAsync(AppDbContext dbContext, SeasonMonthMappingService seasonService)
        {
            Console.WriteLine("--- Hunter Scenario (Default Mappings) ---");

            var property = await CreateTestPropertyAsync(dbContext, "Hunter Property");
            await CreateFeatureWeightsAsync(dbContext, property.Id);

            // Test various dates throughout hunting season
            var testDates = new[]
            {
                new DateTime(2024, 9, 15),   // September - Early Season
                new DateTime(2024, 10, 15),  // October - Early Season/Pre-Rut overlap
                new DateTime(2024, 11, 15),  // November - Rut
                new DateTime(2024, 12, 15),  // December - Post-Rut/Late Season overlap
                new DateTime(2024, 1, 15),   // January - Late Season
            };

            foreach (var date in testDates)
            {
                var activeSeasons = await seasonService.GetActiveSeasonsForDateAsync(date, property);
                var primarySeason = await seasonService.GetPrimarySeasonForDateAsync(date, property);
                var results = await GetFeatureWeights.HandleAsync(dbContext, seasonService, property.Id, date);

                Console.WriteLine($"  Date: {date:yyyy-MM-dd} | Primary Season: {primarySeason} | Active Seasons: [{string.Join(", ", activeSeasons)}] | Results: {results.Count}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Test scenario: Land manager with selective season overrides
        /// </summary>
        private static async Task TestLandManagerScenarioAsync(AppDbContext dbContext, SeasonMonthMappingService seasonService)
        {
            Console.WriteLine("--- Land Manager Scenario (Selective Overrides) ---");

            var property = await CreateTestPropertyAsync(dbContext, "Land Manager Property");
            await CreateFeatureWeightsAsync(dbContext, property.Id);

            // Land manager extends early season for harvest management
            await seasonService.SetPropertySeasonOverrideAsync(property.Id, Season.EarlySeason, new[] { 8, 9, 10, 11 });
            await seasonService.SetPropertySeasonOverrideAsync(property.Id, Season.LateSeason, new[] { 12, 1, 2 });

            var testDates = new[]
            {
                new DateTime(2024, 8, 15),   // August - Extended Early Season
                new DateTime(2024, 10, 15),  // October - Extended Early Season (overrides default)
                new DateTime(2024, 11, 15),  // November - Extended Early Season/Rut overlap
                new DateTime(2024, 2, 15),   // February - Extended Late Season
            };

            foreach (var date in testDates)
            {
                var activeSeasons = await seasonService.GetActiveSeasonsForDateAsync(date, property);
                var primarySeason = await seasonService.GetPrimarySeasonForDateAsync(date, property);
                var results = await GetFeatureWeights.HandleAsync(dbContext, seasonService, property.Id, date);

                Console.WriteLine($"  Date: {date:yyyy-MM-dd} | Primary Season: {primarySeason} | Active Seasons: [{string.Join(", ", activeSeasons)}] | Results: {results.Count}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Test scenario: Researcher with extensive custom season definitions
        /// </summary>
        private static async Task TestResearcherScenarioAsync(AppDbContext dbContext, SeasonMonthMappingService seasonService)
        {
            Console.WriteLine("--- Researcher Scenario (Extensive Customization) ---");

            var property = await CreateTestPropertyAsync(dbContext, "Research Property");
            await CreateFeatureWeightsAsync(dbContext, property.Id);

            // Researcher studying rut timing variations - custom study periods
            await seasonService.SetPropertySeasonOverrideAsync(property.Id, Season.PreRut, new[] { 9, 10 });
            await seasonService.SetPropertySeasonOverrideAsync(property.Id, Season.Rut, new[] { 10, 11, 12 }); // Extended rut study
            await seasonService.SetPropertySeasonOverrideAsync(property.Id, Season.PostRut, new[] { 1, 2, 3 }); // Extended post-rut study
            await seasonService.SetPropertySeasonOverrideAsync(property.Id, Season.LateSeason, new[] { 2, 3, 4 }); // Different late season

            var testDates = new[]
            {
                new DateTime(2024, 9, 15),   // September - Pre-Rut study
                new DateTime(2024, 10, 15),  // October - Pre-Rut/Rut overlap
                new DateTime(2024, 12, 15),  // December - Extended Rut study
                new DateTime(2024, 2, 15),   // February - Post-Rut/Late Season overlap
                new DateTime(2024, 6, 15),   // June - Should only match YearRound
            };

            foreach (var date in testDates)
            {
                var activeSeasons = await seasonService.GetActiveSeasonsForDateAsync(date, property);
                var primarySeason = await seasonService.GetPrimarySeasonForDateAsync(date, property);
                var results = await GetFeatureWeights.HandleAsync(dbContext, seasonService, property.Id, date);

                Console.WriteLine($"  Date: {date:yyyy-MM-dd} | Primary Season: {primarySeason} | Active Seasons: [{string.Join(", ", activeSeasons)}] | Results: {results.Count}");
            }

            Console.WriteLine();
        }

        /// <summary>
        /// Test edge cases and error scenarios
        /// </summary>
        private static async Task TestEdgeCasesAsync(AppDbContext dbContext, SeasonMonthMappingService seasonService)
        {
            Console.WriteLine("--- Edge Cases and Error Scenarios ---");

            var property = await CreateTestPropertyAsync(dbContext, "Edge Case Property");
            await CreateFeatureWeightsAsync(dbContext, property.Id);

            // Test 1: Create conflicting overlaps
            await seasonService.SetPropertySeasonOverrideAsync(property.Id, Season.EarlySeason, new[] { 9, 10, 11 });
            await seasonService.SetPropertySeasonOverrideAsync(property.Id, Season.PreRut, new[] { 10, 11 });
            await seasonService.SetPropertySeasonOverrideAsync(property.Id, Season.Rut, new[] { 11 });

            var conflictDate = new DateTime(2024, 11, 15); // November - should match 3 seasons + YearRound
            var conflictSeasons = await seasonService.GetActiveSeasonsForDateAsync(conflictDate, property);
            var conflictPrimary = await seasonService.GetPrimarySeasonForDateAsync(conflictDate, property);

            Console.WriteLine($"  Conflict Test - Date: {conflictDate:yyyy-MM-dd} | Primary: {conflictPrimary} | Active: [{string.Join(", ", conflictSeasons)}]");

            // Test 2: Invalid property ID
            try
            {
                await GetFeatureWeights.HandleAsync(dbContext, seasonService, 99999, conflictDate);
                Console.WriteLine("  ERROR: Should have thrown exception for invalid property ID");
            }
            catch (InvalidOperationException ex)
            {
                Console.WriteLine($"  ✓ Correctly handled invalid property ID: {ex.Message}");
            }

            // Test 3: Create property with no matching seasons for a specific month
            var isolatedProperty = await CreateTestPropertyAsync(dbContext, "Isolated Property");
            
            // Override all seasons to exclude June
            await seasonService.SetPropertySeasonOverrideAsync(isolatedProperty.Id, Season.EarlySeason, new[] { 9, 10 });
            await seasonService.SetPropertySeasonOverrideAsync(isolatedProperty.Id, Season.PreRut, new[] { 10 });
            await seasonService.SetPropertySeasonOverrideAsync(isolatedProperty.Id, Season.Rut, new[] { 11 });
            await seasonService.SetPropertySeasonOverrideAsync(isolatedProperty.Id, Season.PostRut, new[] { 12 });
            await seasonService.SetPropertySeasonOverrideAsync(isolatedProperty.Id, Season.LateSeason, new[] { 1 });
            await seasonService.SetPropertySeasonOverrideAsync(isolatedProperty.Id, Season.YearRound, new[] { 2, 3, 4, 5 }); // Exclude June

            var isolatedDate = new DateTime(2024, 6, 15); // June
            var isolatedSeasons = await seasonService.GetActiveSeasonsForDateAsync(isolatedDate, isolatedProperty);
            var isolatedPrimary = await seasonService.GetPrimarySeasonForDateAsync(isolatedDate, isolatedProperty);

            Console.WriteLine($"  Isolation Test - Date: {isolatedDate:yyyy-MM-dd} | Primary: {isolatedPrimary ?? null} | Active: [{string.Join(", ", isolatedSeasons)}]");

            Console.WriteLine();
        }

        private static AppDbContext CreateInMemoryDbContext()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            return new AppDbContext(options);
        }

        private static async Task<Property> CreateTestPropertyAsync(AppDbContext dbContext, string name)
        {
            var point = new Point(0, 0) { SRID = 4326 };
            var property = new Property(name, point, null, "UTC", 6, 20);

            dbContext.Properties.Add(property);
            await dbContext.SaveChangesAsync();
            return property;
        }

        private static async Task CreateFeatureWeightsAsync(AppDbContext dbContext, int propertyId)
        {
            var featureWeights = new[]
            {
                new FeatureWeight(
                    propertyId,
                    ClassificationType.FoodPlot,
                    0.5f,
                    0.6f,
                    new Dictionary<Season, float>
                    {
                        { Season.EarlySeason, 0.3f },
                        { Season.PreRut, 0.4f },
                        { Season.Rut, 0.8f },
                        { Season.PostRut, 0.6f },
                        { Season.LateSeason, 0.4f }
                    }),
                new FeatureWeight(
                    propertyId,
                    ClassificationType.Creek,
                    0.7f,
                    null,
                    new Dictionary<Season, float>
                    {
                        { Season.EarlySeason, 0.6f },
                        { Season.PreRut, 0.7f },
                        { Season.Rut, 0.9f },
                        { Season.PostRut, 0.8f },
                        { Season.LateSeason, 0.7f }
                    }),
                new FeatureWeight(
                    propertyId,
                    ClassificationType.BeddingArea,
                    0.8f)
            };

            dbContext.FeatureWeights.AddRange(featureWeights);
            await dbContext.SaveChangesAsync();
        }
    }
}