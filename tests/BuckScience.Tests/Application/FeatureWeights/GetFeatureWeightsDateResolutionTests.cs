using BuckScience.Application.Abstractions;
using BuckScience.Application.FeatureWeights;
using BuckScience.Application.Services;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using BuckScience.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System;
using System.Linq;
using System.Threading.Tasks;
using Xunit;

namespace BuckScience.Tests.Application.FeatureWeights
{
    public class GetFeatureWeightsDateResolutionTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly SeasonMonthMappingService _seasonMappingService;

        public GetFeatureWeightsDateResolutionTests()
        {
            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _seasonMappingService = new SeasonMonthMappingService(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        [Fact]
        public async Task HandleAsync_WithDate_InvalidPropertyId_ThrowsInvalidOperationException()
        {
            // Arrange
            var invalidPropertyId = 999;
            var date = new DateTime(2024, 11, 15); // November

            // Act & Assert
            var exception = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                GetFeatureWeights.HandleAsync(_dbContext, _seasonMappingService, invalidPropertyId, date));

            Assert.Contains("Property with ID 999 not found", exception.Message);
        }

        [Fact]
        public async Task HandleAsync_WithDate_DefaultSeasonMapping_ResolvesCorrectSeason()
        {
            // Arrange
            var property = await CreateTestPropertyWithFeatureWeightsAsync();
            var novemberDate = new DateTime(2024, 11, 15); // November should resolve to Rut season

            // Act
            var results = await GetFeatureWeights.HandleAsync(_dbContext, _seasonMappingService, property.Id, novemberDate);

            // Assert
            Assert.NotEmpty(results);
            
            // Verify that the results use the Rut season for effective weight calculation
            var firstResult = results.First();
            Assert.NotNull(firstResult.SeasonalWeights);
            
            // Since we set up seasonal weights, the effective weight should come from Rut season
            if (firstResult.SeasonalWeights.ContainsKey(Season.Rut))
            {
                Assert.Equal(firstResult.SeasonalWeights[Season.Rut], firstResult.EffectiveWeight);
            }
        }

        [Fact]
        public async Task HandleAsync_WithDate_PropertyOverride_ResolvesCustomSeason()
        {
            // Arrange
            var property = await CreateTestPropertyWithFeatureWeightsAsync();
            
            // Create custom override: Rut season extends into December for this property
            await _seasonMappingService.SetPropertySeasonOverrideAsync(property.Id, Season.Rut, new[] { 11, 12 });
            
            var decemberDate = new DateTime(2024, 12, 15); // December should now resolve to Rut (not PostRut)

            // Act
            var results = await GetFeatureWeights.HandleAsync(_dbContext, _seasonMappingService, property.Id, decemberDate);

            // Assert
            Assert.NotEmpty(results);
            
            // Verify that the results use the extended Rut season for effective weight calculation
            var firstResult = results.First();
            Assert.NotNull(firstResult.SeasonalWeights);
            
            if (firstResult.SeasonalWeights.ContainsKey(Season.Rut))
            {
                Assert.Equal(firstResult.SeasonalWeights[Season.Rut], firstResult.EffectiveWeight);
            }
        }

        [Fact]
        public async Task HandleAsync_WithDate_NoMatchingSeason_FallsBackToUserOrDefaultWeight()
        {
            // Arrange
            var property = await CreateTestPropertyWithFeatureWeightsAsync();
            
            // Use a date that doesn't match any season in our test setup
            // First, override all seasons to not include June
            await _seasonMappingService.SetPropertySeasonOverrideAsync(property.Id, Season.EarlySeason, new[] { 9, 10 });
            await _seasonMappingService.SetPropertySeasonOverrideAsync(property.Id, Season.PreRut, new[] { 10 });
            await _seasonMappingService.SetPropertySeasonOverrideAsync(property.Id, Season.Rut, new[] { 11 });
            await _seasonMappingService.SetPropertySeasonOverrideAsync(property.Id, Season.PostRut, new[] { 12 });
            await _seasonMappingService.SetPropertySeasonOverrideAsync(property.Id, Season.LateSeason, new[] { 1 });
            // Don't override YearRound, which should still cover June
            
            var juneDate = new DateTime(2024, 6, 15); // June

            // Act
            var results = await GetFeatureWeights.HandleAsync(_dbContext, _seasonMappingService, property.Id, juneDate);

            // Assert
            Assert.NotEmpty(results);
            
            // Should still return results, just with YearRound season or fallback weights
            var firstResult = results.First();
            Assert.True(firstResult.EffectiveWeight > 0);
        }

        [Theory]
        [InlineData(1, Season.LateSeason)] // January - Late Season
        [InlineData(9, Season.EarlySeason)] // September - Early Season  
        [InlineData(10, Season.EarlySeason)] // October - Multiple matches, but EarlySeason comes first (enum value 1)
        [InlineData(11, Season.Rut)] // November - Rut
        [InlineData(12, Season.PostRut)] // December - Multiple matches, but PostRut comes first (enum value 4 vs LateSeason 5)
        public async Task HandleAsync_WithDate_DefaultMappings_ResolvesExpectedPrimarySeason(int month, Season expectedPrimarySeason)
        {
            // Arrange
            var property = await CreateTestPropertyWithFeatureWeightsAsync();
            var date = new DateTime(2024, month, 15);

            // Act - Use the service directly to verify season resolution
            var activeSeason = await _seasonMappingService.GetPrimarySeasonForDateAsync(date, property);

            // Assert
            Assert.Equal(expectedPrimarySeason, activeSeason);
        }

        [Fact]
        public async Task HandleAsync_WithDate_MultipleUserTypes_AllReturnValidResults()
        {
            // Test for hunter, land manager, and researcher user types
            // This simulates different properties that might have different configurations
            
            // Arrange - Hunter property with default settings
            var hunterProperty = await CreateTestPropertyWithFeatureWeightsAsync("Hunter Property");
            
            // Arrange - Land manager property with some custom overrides
            var landManagerProperty = await CreateTestPropertyWithFeatureWeightsAsync("Land Manager Property");
            await _seasonMappingService.SetPropertySeasonOverrideAsync(landManagerProperty.Id, Season.EarlySeason, new[] { 8, 9, 10, 11 }); // Extended early season
            
            // Arrange - Researcher property with extensive custom overrides
            var researcherProperty = await CreateTestPropertyWithFeatureWeightsAsync("Researcher Property");
            await _seasonMappingService.SetPropertySeasonOverrideAsync(researcherProperty.Id, Season.Rut, new[] { 10, 11, 12 }); // Extended rut study period
            await _seasonMappingService.SetPropertySeasonOverrideAsync(researcherProperty.Id, Season.PostRut, new[] { 1, 2 }); // Different post-rut period
            
            var testDate = new DateTime(2024, 11, 15); // November

            // Act
            var hunterResults = await GetFeatureWeights.HandleAsync(_dbContext, _seasonMappingService, hunterProperty.Id, testDate);
            var landManagerResults = await GetFeatureWeights.HandleAsync(_dbContext, _seasonMappingService, landManagerProperty.Id, testDate);
            var researcherResults = await GetFeatureWeights.HandleAsync(_dbContext, _seasonMappingService, researcherProperty.Id, testDate);

            // Assert
            Assert.NotEmpty(hunterResults);
            Assert.NotEmpty(landManagerResults);
            Assert.NotEmpty(researcherResults);
            
            // All should return the same number of feature types
            Assert.Equal(hunterResults.Count, landManagerResults.Count);
            Assert.Equal(hunterResults.Count, researcherResults.Count);
            
            // Effective weights may differ due to different season resolutions
            // For November: Hunter uses default Rut, Land Manager might use extended EarlySeason, Researcher uses extended Rut
        }

        private async Task<Property> CreateTestPropertyWithFeatureWeightsAsync(string name = "Test Property")
        {
            var point = new Point(0, 0) { SRID = 4326 };
            var property = new Property(name, point, null, "UTC", 6, 20);

            _dbContext.Properties.Add(property);
            await _dbContext.SaveChangesAsync();

            // Create some test feature weights with seasonal variations
            var featureWeight1 = new FeatureWeight(
                property.Id,
                ClassificationType.FoodPlot,
                0.5f, // default weight
                0.6f, // user weight
                new Dictionary<Season, float>
                {
                    { Season.EarlySeason, 0.3f },
                    { Season.PreRut, 0.4f },
                    { Season.Rut, 0.8f },
                    { Season.PostRut, 0.6f },
                    { Season.LateSeason, 0.4f }
                });

            var featureWeight2 = new FeatureWeight(
                property.Id,
                ClassificationType.Creek,
                0.7f, // default weight
                null, // no user override
                new Dictionary<Season, float>
                {
                    { Season.EarlySeason, 0.6f },
                    { Season.PreRut, 0.7f },
                    { Season.Rut, 0.9f },
                    { Season.PostRut, 0.8f },
                    { Season.LateSeason, 0.7f }
                });

            _dbContext.FeatureWeights.AddRange(featureWeight1, featureWeight2);
            await _dbContext.SaveChangesAsync();

            return property;
        }
    }
}