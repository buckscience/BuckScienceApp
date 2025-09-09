using BuckScience.Application.FeatureWeights;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using BuckScience.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Xunit;

namespace BuckScience.Tests.Application.FeatureWeights;

public class UpdateDefaultFeatureWeightsTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GeometryFactory _geometryFactory;

    public UpdateDefaultFeatureWeightsTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _geometryFactory = new GeometryFactory();
    }

    [Fact]
    public async Task UpdateDefaultFeatureWeights_UpdatesDefaultWeightsCorrectly()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = 1,
            AzureEntraB2CId = "test-user-id",
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            Email = "testuser@example.com"
        };
        
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
        
        _context.ApplicationUsers.Add(user);
        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        // Create materialized feature weights
        var featureWeight = new FeatureWeight(
            property.Id,
            ClassificationType.BeddingArea,
            0.9f, // original default
            userWeight: null,
            seasonalWeights: null,
            isCustom: false);
        
        _context.FeatureWeights.Add(featureWeight);
        await _context.SaveChangesAsync();

        var newDefaultWeights = new Dictionary<ClassificationType, float>
        {
            { ClassificationType.BeddingArea, 0.95f }
        };
        var command = new UpdateDefaultFeatureWeights.Command(newDefaultWeights);

        // Act
        var result = await UpdateDefaultFeatureWeights.HandleAsync(command, _context, property.Id, CancellationToken.None);

        // Assert
        Assert.True(result);
        
        var updatedFeatureWeight = await _context.FeatureWeights
            .FirstAsync(fw => fw.PropertyId == property.Id && fw.ClassificationType == ClassificationType.BeddingArea);
        
        Assert.Equal(0.95f, updatedFeatureWeight.DefaultWeight);
        Assert.False(updatedFeatureWeight.IsCustom); // Should remain false since we're updating defaults
    }

    [Fact]
    public async Task UpdateDefaultFeatureWeights_ValidatesWeightRange()
    {
        // Arrange
        var user = new ApplicationUser
        {
            Id = 1,
            AzureEntraB2CId = "test-user-id",
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            Email = "testuser@example.com"
        };
        
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
        
        _context.ApplicationUsers.Add(user);
        _context.Properties.Add(property);
        await _context.SaveChangesAsync();

        var invalidWeights = new Dictionary<ClassificationType, float>
        {
            { ClassificationType.BeddingArea, 1.5f } // Invalid: > 1.0
        };
        var command = new UpdateDefaultFeatureWeights.Command(invalidWeights);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentException>(
            () => UpdateDefaultFeatureWeights.HandleAsync(command, _context, property.Id, CancellationToken.None));
        
        Assert.Contains("must be between 0 and 1", exception.Message);
    }

    [Fact]
    public async Task UpdateDefaultFeatureWeights_ReturnsFalseForNonExistentProperty()
    {
        // Arrange
        var weights = new Dictionary<ClassificationType, float>
        {
            { ClassificationType.BeddingArea, 0.8f }
        };
        var command = new UpdateDefaultFeatureWeights.Command(weights);

        // Act
        var result = await UpdateDefaultFeatureWeights.HandleAsync(command, _context, 9999, CancellationToken.None);

        // Assert
        Assert.False(result);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}