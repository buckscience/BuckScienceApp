using BuckScience.Application.Abstractions;
using BuckScience.Application.Services;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace BuckScience.Tests.Application
{
    public class SeasonMonthMappingServiceTests
    {
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
            var mockDbContext = new Mock<IAppDbContext>();
            var service = new SeasonMonthMappingService(mockDbContext.Object);
            var season = Season.PreRut;

            // Act
            var result = await service.GetMonthsForPropertyAsync(season, null!);

            // Assert
            Assert.Equal(season.GetDefaultMonths(), result);
        }

        [Fact]
        public void SeasonMonthMappingService_CanBeInstantiated_WithValidDbContext()
        {
            // Arrange
            var mockDbContext = new Mock<IAppDbContext>();

            // Act
            var service = new SeasonMonthMappingService(mockDbContext.Object);

            // Assert
            Assert.NotNull(service);
        }

        // Note: Testing the async database operations would require either:
        // 1. An in-memory database for integration testing
        // 2. More complex EF Core mocking setup with IAsyncQueryProvider
        // 3. Separating the database logic into a repository pattern for easier testing
        // For this implementation, we're focusing on the core business logic tests
        // in the Domain layer (which are comprehensive) and basic service instantiation tests here.
    }
}