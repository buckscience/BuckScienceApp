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