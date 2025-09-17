using BuckScience.Application.Abstractions;
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

namespace BuckScience.Tests.Application
{
    public class SeasonMonthMappingServiceTests : IDisposable
    {
        private readonly AppDbContext _dbContext;
        private readonly SeasonMonthMappingService _service;

        public SeasonMonthMappingServiceTests()
        {
            // Create in-memory database for testing
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _dbContext = new AppDbContext(options);
            _service = new SeasonMonthMappingService(_dbContext);
        }

        public void Dispose()
        {
            _dbContext.Dispose();
        }

        [Fact]
        public void SeasonMonthMappingService_Constructor_WithNullDbContext_ThrowsArgumentNullException()
        {
            // Arrange, Act & Assert
            Assert.Throws<ArgumentNullException>(() => new SeasonMonthMappingService(null!));
        }

        [Fact]
        public async Task GetMonthsForPropertyAsync_WithNullProperty_ReturnsDefaultMonths()
        {
            // Arrange
            var season = Season.PreRut;

            // Act
            var result = await _service.GetMonthsForPropertyAsync(season, null!);

            // Assert
            Assert.Equal(season.GetDefaultMonths(), result);
        }

        [Fact]
        public async Task GetMonthsForPropertyAsync_WithPropertyNoOverride_ReturnsDefaultMonths()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            var season = Season.Rut;

            // Act
            var result = await _service.GetMonthsForPropertyAsync(season, property);

            // Assert
            Assert.Equal(season.GetDefaultMonths(), result);
            Assert.Equal(new[] { 11 }, result); // Rut default is November
        }

        [Fact]
        public async Task GetMonthsForPropertyAsync_WithPropertyOverride_ReturnsOverrideMonths()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            var season = Season.PreRut;
            var customMonths = new[] { 9, 10 }; // Custom override instead of default October only

            // Create override
            await _service.SetPropertySeasonOverrideAsync(property.Id, season, customMonths);

            // Act
            var result = await _service.GetMonthsForPropertyAsync(season, property);

            // Assert
            Assert.Equal(customMonths, result);
            Assert.NotEqual(season.GetDefaultMonths(), result); // Should be different from default
        }

        [Fact]
        public async Task SetPropertySeasonOverrideAsync_CreatesNewOverride_WhenNoneExists()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            var season = Season.EarlySeason;
            var customMonths = new[] { 8, 9, 10 }; // Extended early season

            // Act
            var result = await _service.SetPropertySeasonOverrideAsync(property.Id, season, customMonths);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(property.Id, result.PropertyId);
            Assert.Equal(season, result.Season);
            Assert.Equal(customMonths, result.GetMonths());

            // Verify in database
            var storedOverride = await _dbContext.PropertySeasonMonthsOverrides
                .FirstOrDefaultAsync(o => o.PropertyId == property.Id && o.Season == season);
            Assert.NotNull(storedOverride);
            Assert.Equal(customMonths, storedOverride.GetMonths());
        }

        [Fact]
        public async Task SetPropertySeasonOverrideAsync_UpdatesExistingOverride_WhenOneExists()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            var season = Season.LateSeason;
            var originalMonths = new[] { 12 }; // Original override
            var updatedMonths = new[] { 12, 1, 2 }; // Extended late season

            // Create initial override
            await _service.SetPropertySeasonOverrideAsync(property.Id, season, originalMonths);

            // Act - Update the override
            var result = await _service.SetPropertySeasonOverrideAsync(property.Id, season, updatedMonths);

            // Assert
            Assert.NotNull(result);
            Assert.Equal(updatedMonths, result.GetMonths());

            // Verify only one override exists in database
            var overrides = await _dbContext.PropertySeasonMonthsOverrides
                .Where(o => o.PropertyId == property.Id && o.Season == season)
                .ToListAsync();
            Assert.Single(overrides);
            Assert.Equal(updatedMonths, overrides[0].GetMonths());
        }

        [Fact]
        public async Task RemovePropertySeasonOverrideAsync_RemovesOverride_WhenExists()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            var season = Season.PostRut;
            var customMonths = new[] { 12, 1 };

            // Create override
            await _service.SetPropertySeasonOverrideAsync(property.Id, season, customMonths);

            // Act
            var result = await _service.RemovePropertySeasonOverrideAsync(property.Id, season);

            // Assert
            Assert.True(result);

            // Verify override is removed from database
            var storedOverride = await _dbContext.PropertySeasonMonthsOverrides
                .FirstOrDefaultAsync(o => o.PropertyId == property.Id && o.Season == season);
            Assert.Null(storedOverride);

            // Verify service now returns default months
            var months = await _service.GetMonthsForPropertyAsync(season, property);
            Assert.Equal(season.GetDefaultMonths(), months);
        }

        [Fact]
        public async Task RemovePropertySeasonOverrideAsync_ReturnsFalse_WhenNoOverrideExists()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            var season = Season.Rut;

            // Act
            var result = await _service.RemovePropertySeasonOverrideAsync(property.Id, season);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task GetAllPropertyOverridesAsync_ReturnsAllOverrides_ForProperty()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            var preRutOverride = new[] { 9, 10 };
            var rutOverride = new[] { 10, 11, 12 };
            var lateSeasonOverride = new[] { 1, 2 };

            // Create multiple overrides
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.PreRut, preRutOverride);
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.Rut, rutOverride);
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.LateSeason, lateSeasonOverride);

            // Act
            var result = await _service.GetAllPropertyOverridesAsync(property.Id);

            // Assert
            Assert.Equal(3, result.Count);
            Assert.Equal(preRutOverride, result[Season.PreRut]);
            Assert.Equal(rutOverride, result[Season.Rut]);
            Assert.Equal(lateSeasonOverride, result[Season.LateSeason]);
        }

        [Fact]
        public async Task GetAllPropertyOverridesAsync_ReturnsEmptyDictionary_WhenNoOverrides()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();

            // Act
            var result = await _service.GetAllPropertyOverridesAsync(property.Id);

            // Assert
            Assert.Empty(result);
        }

        [Fact]
        public async Task HybridLogic_FallsBackToDefaults_WhenOverrideIsInvalid()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            var season = Season.YearRound;

            // Create override with valid months
            var validOverride = await _service.SetPropertySeasonOverrideAsync(property.Id, season, new[] { 6, 7, 8 });

            // Simulate corrupted override by setting empty months
            validOverride.SetMonths(null);
            await _dbContext.SaveChangesAsync();

            // Act
            var result = await _service.GetMonthsForPropertyAsync(season, property);

            // Assert
            Assert.Equal(season.GetDefaultMonths(), result); // Should fall back to defaults
        }

        [Theory]
        [InlineData(Season.EarlySeason, new[] { 9, 10 })]
        [InlineData(Season.PreRut, new[] { 10 })]
        [InlineData(Season.Rut, new[] { 11 })]
        [InlineData(Season.PostRut, new[] { 12 })]
        [InlineData(Season.LateSeason, new[] { 12, 1 })]
        [InlineData(Season.YearRound, new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 })]
        public async Task GetMonthsForPropertyAsync_WithoutOverrides_ReturnsCorrectDefaults(Season season, int[] expectedMonths)
        {
            // Arrange
            var property = await CreateTestPropertyAsync();

            // Act
            var result = await _service.GetMonthsForPropertyAsync(season, property);

            // Assert
            Assert.Equal(expectedMonths, result);
        }

        [Fact]
        public async Task SeasonMonthMappingService_SupportsMultipleProperties_WithDifferentOverrides()
        {
            // Arrange
            var property1 = await CreateTestPropertyAsync();
            var property2 = await CreateTestPropertyAsync();
            var season = Season.PreRut;
            var property1Override = new[] { 9, 10 };
            var property2Override = new[] { 10, 11 };

            // Create different overrides for each property
            await _service.SetPropertySeasonOverrideAsync(property1.Id, season, property1Override);
            await _service.SetPropertySeasonOverrideAsync(property2.Id, season, property2Override);

            // Act
            var result1 = await _service.GetMonthsForPropertyAsync(season, property1);
            var result2 = await _service.GetMonthsForPropertyAsync(season, property2);

            // Assert
            Assert.Equal(property1Override, result1);
            Assert.Equal(property2Override, result2);
            Assert.NotEqual(result1, result2); // Different properties have different overrides
        }

        [Fact]
        public async Task GetActiveSeasonsForDateAsync_WithDefaultMappings_ReturnsCorrectSeasons()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            var octoberDate = new DateTime(2024, 10, 15); // October should match EarlySeason, PreRut, and YearRound

            // Act
            var activeSeasons = await _service.GetActiveSeasonsForDateAsync(octoberDate, property);

            // Assert
            Assert.Contains(Season.EarlySeason, activeSeasons); // EarlySeason includes October (9,10)
            Assert.Contains(Season.PreRut, activeSeasons); // PreRut includes October (10)
            Assert.Contains(Season.YearRound, activeSeasons); // YearRound includes all months
            Assert.Equal(3, activeSeasons.Count); // Should be exactly these three seasons
            
            // Verify ordering by enum value
            Assert.Equal(Season.EarlySeason, activeSeasons[0]); // EarlySeason = 1
            Assert.Equal(Season.PreRut, activeSeasons[1]); // PreRut = 2
            Assert.Equal(Season.YearRound, activeSeasons[2]); // YearRound = 6
        }

        [Fact]
        public async Task GetActiveSeasonsForDateAsync_WithPropertyOverride_ReturnsCustomSeasons()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            var decemberDate = new DateTime(2024, 12, 15);
            
            // Create custom override - extend Rut season into December
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.Rut, new[] { 11, 12 });

            // Act
            var activeSeasons = await _service.GetActiveSeasonsForDateAsync(decemberDate, property);

            // Assert
            // December should now match Rut (custom override), PostRut (default), LateSeason (default), and YearRound (default)
            Assert.Contains(Season.Rut, activeSeasons); // Custom override
            Assert.Contains(Season.PostRut, activeSeasons); // Default includes December
            Assert.Contains(Season.LateSeason, activeSeasons); // Default includes December
            Assert.Contains(Season.YearRound, activeSeasons); // Default includes all months
            Assert.Equal(4, activeSeasons.Count);
        }

        [Fact]
        public async Task GetActiveSeasonsForDateAsync_NoMatchingSeasons_ReturnsEmptyList()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            
            // Override all seasons to exclude June
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.EarlySeason, new[] { 9, 10 });
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.PreRut, new[] { 10 });
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.Rut, new[] { 11 });
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.PostRut, new[] { 12 });
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.LateSeason, new[] { 1 });
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.YearRound, new[] { 2, 3, 4, 5 }); // Exclude June

            var juneDate = new DateTime(2024, 6, 15); // June

            // Act
            var activeSeasons = await _service.GetActiveSeasonsForDateAsync(juneDate, property);

            // Assert
            Assert.Empty(activeSeasons);
        }

        [Fact]
        public async Task GetPrimarySeasonForDateAsync_WithMultipleMatches_ReturnsFirstByEnumOrder()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            var octoberDate = new DateTime(2024, 10, 15); // Matches both EarlySeason (1) and PreRut (2)

            // Act
            var primarySeason = await _service.GetPrimarySeasonForDateAsync(octoberDate, property);

            // Assert
            Assert.Equal(Season.EarlySeason, primarySeason); // Should return the first one by enum value
        }

        [Fact]
        public async Task GetPrimarySeasonForDateAsync_NoMatches_ReturnsNull()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            
            // Override all seasons to exclude March
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.EarlySeason, new[] { 9, 10 });
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.PreRut, new[] { 10 });
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.Rut, new[] { 11 });
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.PostRut, new[] { 12 });
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.LateSeason, new[] { 1 });
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.YearRound, new[] { 2, 4, 5, 6, 7, 8 }); // Exclude March

            var marchDate = new DateTime(2024, 3, 15); // March

            // Act
            var primarySeason = await _service.GetPrimarySeasonForDateAsync(marchDate, property);

            // Assert
            Assert.Null(primarySeason);
        }

        [Theory]
        [InlineData(1, Season.LateSeason)] // January - LateSeason (12,1)
        [InlineData(9, Season.EarlySeason)] // September - EarlySeason (9,10)
        [InlineData(10, Season.EarlySeason)] // October - Multiple matches, but EarlySeason comes first
        [InlineData(11, Season.Rut)] // November - Rut (11)
        [InlineData(12, Season.PostRut)] // December - Multiple matches, but PostRut comes first
        public async Task GetPrimarySeasonForDateAsync_DefaultMappings_ReturnsExpectedSeason(int month, Season expectedSeason)
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            var date = new DateTime(2024, month, 15);

            // Act
            var primarySeason = await _service.GetPrimarySeasonForDateAsync(date, property);

            // Assert
            Assert.Equal(expectedSeason, primarySeason);
        }

        [Fact]
        public async Task DateResolution_CrossYearBoundary_HandlesCorrectly()
        {
            // Test late season spanning December-January
            // Arrange
            var property = await CreateTestPropertyAsync();
            var decemberDate = new DateTime(2024, 12, 15);
            var januaryDate = new DateTime(2025, 1, 15);

            // Act
            var decemberSeasons = await _service.GetActiveSeasonsForDateAsync(decemberDate, property);
            var januarySeasons = await _service.GetActiveSeasonsForDateAsync(januaryDate, property);

            // Assert
            // Both December and January should include LateSeason
            Assert.Contains(Season.LateSeason, decemberSeasons);
            Assert.Contains(Season.LateSeason, januarySeasons);
        }

        [Fact]
        public async Task EdgeCase_ConflictingOverrides_UsesOverrideTable()
        {
            // Arrange
            var property = await CreateTestPropertyAsync();
            
            // Create a conflicting override: set Rut to only November (narrower than some other seasons)
            // and PostRut to November-December (overlapping with Rut's default)
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.Rut, new[] { 11 });
            await _service.SetPropertySeasonOverrideAsync(property.Id, Season.PostRut, new[] { 11, 12 });

            var novemberDate = new DateTime(2024, 11, 15);

            // Act
            var activeSeasons = await _service.GetActiveSeasonsForDateAsync(novemberDate, property);

            // Assert
            // Should find both Rut and PostRut for November based on the overrides
            Assert.Contains(Season.Rut, activeSeasons);
            Assert.Contains(Season.PostRut, activeSeasons);
        }

        private async Task<Property> CreateTestPropertyAsync()
        {
            var point = new Point(0, 0) { SRID = 4326 };
            var property = new Property(
                "Test Property",
                point,
                null,
                "UTC",
                6,
                20);

            _dbContext.Properties.Add(property);
            await _dbContext.SaveChangesAsync();
            return property;
        }
    }
}