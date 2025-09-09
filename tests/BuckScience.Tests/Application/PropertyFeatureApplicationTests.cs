using BuckScience.Application.PropertyFeatures;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using BuckScience.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Tests.Application;

public class PropertyFeatureApplicationTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GeometryFactory _geometryFactory;
    private readonly int _userId;
    private readonly int _propertyId;

    public PropertyFeatureApplicationTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;

        _context = new AppDbContext(options);
        _geometryFactory = new GeometryFactory();
        _userId = 1;

        // Create test user and property
        var user = new ApplicationUser
        {
            Id = _userId,
            AzureEntraB2CId = "test-user-id",
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            Email = "test@example.com"
        };
        _context.ApplicationUsers.Add(user);
        _context.SaveChanges();

        var location = _geometryFactory.CreatePoint(new Coordinate(-94.5, 39.1));
        var property = new Property(
            "Test Property",
            location,
            null, // boundary
            "America/Chicago",
            6, // dayHour
            18 // nightHour
        );
        property.AssignOwner(user.Id);
        _context.Properties.Add(property);
        _context.SaveChanges();

        _propertyId = property.Id;
    }

    [Fact]
    public async Task CreatePropertyFeature_WithWeight_CreatesFeatureWithWeight()
    {
        // Arrange
        const float weight = 0.8f;
        var command = new CreatePropertyFeature.Command(
            PropertyId: _propertyId,
            ClassificationType: ClassificationType.FoodPlot,
            GeometryWkt: "POINT(-94.5 39.1)",
            Name: "Test Food Plot",
            Notes: "Test notes",
            Weight: weight);

        // Act
        var featureId = await CreatePropertyFeature.HandleAsync(
            command, _context, _geometryFactory, _userId, CancellationToken.None);

        // Assert
        var feature = await _context.PropertyFeatures.FindAsync(featureId);
        Assert.NotNull(feature);
        Assert.Equal(weight, feature.Weight);
        Assert.Equal("Test Food Plot", feature.Name);
        Assert.Equal("Test notes", feature.Notes);
        Assert.Equal(ClassificationType.FoodPlot, feature.ClassificationType);
    }

    [Fact]
    public async Task CreatePropertyFeature_WithoutWeight_CreatesFeatureWithNullWeight()
    {
        // Arrange
        var command = new CreatePropertyFeature.Command(
            PropertyId: _propertyId,
            ClassificationType: ClassificationType.BeddingArea,
            GeometryWkt: "POINT(-94.5 39.1)",
            Name: "Test Bedding Area");

        // Act
        var featureId = await CreatePropertyFeature.HandleAsync(
            command, _context, _geometryFactory, _userId, CancellationToken.None);

        // Assert
        var feature = await _context.PropertyFeatures.FindAsync(featureId);
        Assert.NotNull(feature);
        Assert.Null(feature.Weight);
    }

    [Fact]
    public async Task CreatePropertyFeature_WithInvalidWeight_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var command = new CreatePropertyFeature.Command(
            PropertyId: _propertyId,
            ClassificationType: ClassificationType.FoodPlot,
            GeometryWkt: "POINT(-94.5 39.1)",
            Weight: 1.5f);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            CreatePropertyFeature.HandleAsync(
                command, _context, _geometryFactory, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task UpdatePropertyFeature_WithWeight_UpdatesWeight()
    {
        // Arrange
        // First create a feature
        var createCommand = new CreatePropertyFeature.Command(
            PropertyId: _propertyId,
            ClassificationType: ClassificationType.Creek,
            GeometryWkt: "POINT(-94.5 39.1)",
            Weight: 0.5f);

        var featureId = await CreatePropertyFeature.HandleAsync(
            createCommand, _context, _geometryFactory, _userId, CancellationToken.None);

        // Update the feature
        var updateCommand = new UpdatePropertyFeature.Command(
            Id: featureId,
            ClassificationType: ClassificationType.Creek,
            GeometryWkt: "POINT(-94.5 39.1)",
            Weight: 0.9f);

        // Act
        var success = await UpdatePropertyFeature.HandleAsync(
            updateCommand, _context, _geometryFactory, _userId, CancellationToken.None);

        // Assert
        Assert.True(success);
        var feature = await _context.PropertyFeatures.FindAsync(featureId);
        Assert.NotNull(feature);
        Assert.Equal(0.9f, feature.Weight);
    }

    [Fact]
    public async Task UpdatePropertyFeature_WithNullWeight_SetsWeightToNull()
    {
        // Arrange
        // First create a feature with weight
        var createCommand = new CreatePropertyFeature.Command(
            PropertyId: _propertyId,
            ClassificationType: ClassificationType.TravelCorridor,
            GeometryWkt: "POINT(-94.5 39.1)",
            Weight: 0.7f);

        var featureId = await CreatePropertyFeature.HandleAsync(
            createCommand, _context, _geometryFactory, _userId, CancellationToken.None);

        // Update the feature to remove weight
        var updateCommand = new UpdatePropertyFeature.Command(
            Id: featureId,
            ClassificationType: ClassificationType.TravelCorridor,
            GeometryWkt: "POINT(-94.5 39.1)",
            Weight: null);

        // Act
        var success = await UpdatePropertyFeature.HandleAsync(
            updateCommand, _context, _geometryFactory, _userId, CancellationToken.None);

        // Assert
        Assert.True(success);
        var feature = await _context.PropertyFeatures.FindAsync(featureId);
        Assert.NotNull(feature);
        Assert.Null(feature.Weight);
    }

    [Fact]
    public async Task UpdatePropertyFeature_WithInvalidWeight_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        // First create a feature
        var createCommand = new CreatePropertyFeature.Command(
            PropertyId: _propertyId,
            ClassificationType: ClassificationType.FieldEdge,
            GeometryWkt: "POINT(-94.5 39.1)");

        var featureId = await CreatePropertyFeature.HandleAsync(
            createCommand, _context, _geometryFactory, _userId, CancellationToken.None);

        // Update with invalid weight
        var updateCommand = new UpdatePropertyFeature.Command(
            Id: featureId,
            ClassificationType: ClassificationType.FieldEdge,
            GeometryWkt: "POINT(-94.5 39.1)",
            Weight: -0.5f);

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentOutOfRangeException>(() =>
            UpdatePropertyFeature.HandleAsync(
                updateCommand, _context, _geometryFactory, _userId, CancellationToken.None));
    }

    [Fact]
    public async Task GetPropertyFeature_ReturnsWeightCorrectly()
    {
        // Arrange
        const float weight = 0.75f;
        var createCommand = new CreatePropertyFeature.Command(
            PropertyId: _propertyId,
            ClassificationType: ClassificationType.BeddingArea,
            GeometryWkt: "POINT(-94.5 39.1)",
            Name: "Test Bedding Area",
            Weight: weight);

        var featureId = await CreatePropertyFeature.HandleAsync(
            createCommand, _context, _geometryFactory, _userId, CancellationToken.None);

        // Act
        var result = await GetPropertyFeature.HandleAsync(
            featureId, _context, _userId, CancellationToken.None);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(weight, result.Weight);
        Assert.Equal("Test Bedding Area", result.Name);
        Assert.Equal(ClassificationType.BeddingArea, result.ClassificationType);
    }

    [Fact]
    public async Task ListPropertyFeatures_ReturnsWeightsCorrectly()
    {
        // Arrange
        // Create multiple features with different weights
        var feature1Command = new CreatePropertyFeature.Command(
            PropertyId: _propertyId,
            ClassificationType: ClassificationType.FoodPlot,
            GeometryWkt: "POINT(-94.5 39.1)",
            Weight: 0.8f);

        var feature2Command = new CreatePropertyFeature.Command(
            PropertyId: _propertyId,
            ClassificationType: ClassificationType.Creek,
            GeometryWkt: "POINT(-94.6 39.2)",
            Weight: null);

        await CreatePropertyFeature.HandleAsync(
            feature1Command, _context, _geometryFactory, _userId, CancellationToken.None);
        await CreatePropertyFeature.HandleAsync(
            feature2Command, _context, _geometryFactory, _userId, CancellationToken.None);

        // Act
        var results = await ListPropertyFeatures.HandleAsync(
            _context, _userId, _propertyId, CancellationToken.None);

        // Assert
        Assert.Equal(2, results.Count);
        
        var foodPlot = results.First(r => r.ClassificationType == ClassificationType.FoodPlot);
        Assert.Equal(0.8f, foodPlot.Weight);

        var waterSource = results.First(r => r.ClassificationType == ClassificationType.Creek);
        Assert.Null(waterSource.Weight);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}