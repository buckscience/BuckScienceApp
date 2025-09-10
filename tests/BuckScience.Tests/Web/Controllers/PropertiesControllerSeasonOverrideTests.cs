using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using BuckScience.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Moq;
using NetTopologySuite.Geometries;
using MockQueryable.Moq;

namespace BuckScience.Tests.Web.Controllers;

public class PropertiesControllerSeasonOverrideTests
{
    private readonly Mock<IAppDbContext> _mockDb;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly Mock<ISeasonMonthMappingService> _mockSeasonMappingService;
    private readonly GeometryFactory _geometryFactory;
    private readonly PropertiesController _controller;

    public PropertiesControllerSeasonOverrideTests()
    {
        _mockDb = new Mock<IAppDbContext>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        _mockSeasonMappingService = new Mock<ISeasonMonthMappingService>();
        _geometryFactory = new GeometryFactory(new PrecisionModel(), 4326);
        
        _controller = new PropertiesController(
            _mockDb.Object,
            _geometryFactory,
            _mockCurrentUser.Object,
            _mockSeasonMappingService.Object);
    }

    [Fact]
    public async Task GetPropertySeasonOverrides_WithValidUser_ReturnsOverrides()
    {
        // Arrange
        var userId = 1;
        var propertyId = 1;
        _mockCurrentUser.Setup(x => x.Id).Returns(userId);

        var property = CreateTestProperty("Test Property", 40.0, -75.0, userId);
        var properties = new List<Property> { property };
        
        var mockPropertySet = properties.AsQueryable().BuildMock();
        _mockDb.Setup(x => x.Properties).Returns(mockPropertySet.Object);

        var overrides = new Dictionary<Season, int[]>
        {
            { Season.EarlySeason, new[] { 8, 9, 10 } }
        };
        _mockSeasonMappingService.Setup(x => x.GetAllPropertyOverridesAsync(propertyId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(overrides);

        // Act
        var result = await _controller.GetPropertySeasonOverrides(propertyId, CancellationToken.None);

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var response = Assert.IsType<PropertiesController.PropertySeasonOverridesResponse>(okResult.Value);
        Assert.Equal(propertyId, response.PropertyId);
        Assert.NotEmpty(response.SeasonOverrides);
        
        var earlySeasonOverride = response.SeasonOverrides.First(s => s.Season == Season.EarlySeason);
        Assert.True(earlySeasonOverride.HasOverride);
        Assert.Equal(new[] { 8, 9, 10 }, earlySeasonOverride.CustomMonths);
    }

    [Fact]
    public async Task GetPropertySeasonOverrides_WithUnauthorizedUser_ReturnsForbid()
    {
        // Arrange
        _mockCurrentUser.Setup(x => x.Id).Returns((int?)null);

        // Act
        var result = await _controller.GetPropertySeasonOverrides(1, CancellationToken.None);

        // Assert
        Assert.IsType<ForbidResult>(result);
    }

    [Fact]
    public async Task GetPropertySeasonOverrides_WithNonExistentProperty_ReturnsNotFound()
    {
        // Arrange
        var userId = 1;
        _mockCurrentUser.Setup(x => x.Id).Returns(userId);

        var properties = new List<Property>();
        var mockPropertySet = properties.AsQueryable().BuildMock();
        _mockDb.Setup(x => x.Properties).Returns(mockPropertySet.Object);

        // Act
        var result = await _controller.GetPropertySeasonOverrides(1, CancellationToken.None);

        // Assert
        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task UpdatePropertySeasonOverrides_WithValidRequest_ReturnsNoContent()
    {
        // Arrange
        var userId = 1;
        var propertyId = 1;
        _mockCurrentUser.Setup(x => x.Id).Returns(userId);

        var property = CreateTestProperty("Test Property", 40.0, -75.0, userId);
        var properties = new List<Property> { property };
        var mockPropertySet = properties.AsQueryable().BuildMock();
        _mockDb.Setup(x => x.Properties).Returns(mockPropertySet.Object);

        var request = new PropertiesController.UpdatePropertySeasonOverridesRequest
        {
            SeasonOverrides = new List<PropertiesController.SeasonOverrideUpdateRequest>
            {
                new() { Season = Season.EarlySeason, CustomMonths = new[] { 8, 9, 10 } }
            }
        };

        _mockSeasonMappingService.Setup(x => x.SetPropertySeasonOverrideAsync(
                propertyId, Season.EarlySeason, new[] { 8, 9, 10 }, It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PropertySeasonMonthsOverride(propertyId, Season.EarlySeason, new[] { 8, 9, 10 }));

        // Act
        var result = await _controller.UpdatePropertySeasonOverrides(propertyId, request, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockSeasonMappingService.Verify(x => x.SetPropertySeasonOverrideAsync(
            propertyId, Season.EarlySeason, new[] { 8, 9, 10 }, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task RemovePropertySeasonOverride_WithValidRequest_ReturnsNoContent()
    {
        // Arrange
        var userId = 1;
        var propertyId = 1;
        _mockCurrentUser.Setup(x => x.Id).Returns(userId);

        var properties = new List<Property> 
        { 
            CreateTestProperty("Test Property", 40.0, -75.0, userId)
        };
        var mockPropertySet = properties.AsQueryable().BuildMock();
        _mockDb.Setup(x => x.Properties).Returns(mockPropertySet.Object);

        _mockSeasonMappingService.Setup(x => x.RemovePropertySeasonOverrideAsync(
                propertyId, Season.EarlySeason, It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.RemovePropertySeasonOverride(propertyId, Season.EarlySeason, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        _mockSeasonMappingService.Verify(x => x.RemovePropertySeasonOverrideAsync(
            propertyId, Season.EarlySeason, It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ResetPropertySeasonOverrides_WithValidRequest_ReturnsNoContent()
    {
        // Arrange
        var userId = 1;
        var propertyId = 1;
        _mockCurrentUser.Setup(x => x.Id).Returns(userId);

        var properties = new List<Property> 
        { 
            CreateTestProperty("Test Property", 40.0, -75.0, userId)
        };
        var mockPropertySet = properties.AsQueryable().BuildMock();
        _mockDb.Setup(x => x.Properties).Returns(mockPropertySet.Object);

        _mockSeasonMappingService.Setup(x => x.RemovePropertySeasonOverrideAsync(
                propertyId, It.IsAny<Season>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);

        // Act
        var result = await _controller.ResetPropertySeasonOverrides(propertyId, CancellationToken.None);

        // Assert
        Assert.IsType<NoContentResult>(result);
        
        // Verify all seasons were reset
        var allSeasons = Enum.GetValues<Season>();
        foreach (var season in allSeasons)
        {
            _mockSeasonMappingService.Verify(x => x.RemovePropertySeasonOverrideAsync(
                propertyId, season, It.IsAny<CancellationToken>()), Times.Once);
        }
    }

    private Property CreateTestProperty(string name, double latitude, double longitude, int userId)
    {
        var point = _geometryFactory.CreatePoint(new Coordinate(longitude, latitude));
        var property = new Property(name, point, null, "UTC", 6, 18);
        property.AssignOwner(userId);
        return property;
    }
}