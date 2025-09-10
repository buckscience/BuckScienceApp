using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace BuckScience.Tests.Domain;

public class PropertyFeatureTests
{
    private readonly Geometry _testGeometry;

    public PropertyFeatureTests()
    {
        var geometryFactory = new GeometryFactory();
        _testGeometry = geometryFactory.CreatePoint(new Coordinate(-94.5, 39.1));
    }

    [Fact]
    public void Constructor_WithWeight_SetsWeightCorrectly()
    {
        // Arrange
        const float weight = 0.8f;

        // Act
        var feature = new PropertyFeature(
            propertyId: 1,
            classificationType: ClassificationType.FoodPlot,
            geometry: _testGeometry,
            weight: weight);

        // Assert
        Assert.Equal(weight, feature.Weight);
    }

    [Fact]
    public void Constructor_WithoutWeight_SetsWeightToNull()
    {
        // Act
        var feature = new PropertyFeature(
            propertyId: 1,
            classificationType: ClassificationType.FoodPlot,
            geometry: _testGeometry);

        // Assert
        Assert.Null(feature.Weight);
    }

    [Fact]
    public void Constructor_WithNullWeight_SetsWeightToNull()
    {
        // Act
        var feature = new PropertyFeature(
            propertyId: 1,
            classificationType: ClassificationType.FoodPlot,
            geometry: _testGeometry,
            weight: null);

        // Assert
        Assert.Null(feature.Weight);
    }

    [Theory]
    [InlineData(0.0f)]
    [InlineData(0.5f)]
    [InlineData(1.0f)]
    public void Constructor_WithValidWeight_SetsWeightCorrectly(float weight)
    {
        // Act
        var feature = new PropertyFeature(
            propertyId: 1,
            classificationType: ClassificationType.FoodPlot,
            geometry: _testGeometry,
            weight: weight);

        // Assert
        Assert.Equal(weight, feature.Weight);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    [InlineData(-1.0f)]
    [InlineData(2.0f)]
    public void Constructor_WithInvalidWeight_ThrowsArgumentOutOfRangeException(float weight)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() =>
            new PropertyFeature(
                propertyId: 1,
                classificationType: ClassificationType.FoodPlot,
                geometry: _testGeometry,
                weight: weight));

        Assert.Contains("Weight must be between 0 and 1", exception.Message);
    }

    [Fact]
    public void SetWeight_WithValidWeight_SetsWeightCorrectly()
    {
        // Arrange
        var feature = new PropertyFeature(
            propertyId: 1,
            classificationType: ClassificationType.FoodPlot,
            geometry: _testGeometry);
        const float weight = 0.7f;

        // Act
        feature.SetWeight(weight);

        // Assert
        Assert.Equal(weight, feature.Weight);
    }

    [Fact]
    public void SetWeight_WithNull_SetsWeightToNull()
    {
        // Arrange
        var feature = new PropertyFeature(
            propertyId: 1,
            classificationType: ClassificationType.FoodPlot,
            geometry: _testGeometry,
            weight: 0.5f);

        // Act
        feature.SetWeight(null);

        // Assert
        Assert.Null(feature.Weight);
    }

    [Theory]
    [InlineData(-0.1f)]
    [InlineData(1.1f)]
    public void SetWeight_WithInvalidWeight_ThrowsArgumentOutOfRangeException(float weight)
    {
        // Arrange
        var feature = new PropertyFeature(
            propertyId: 1,
            classificationType: ClassificationType.FoodPlot,
            geometry: _testGeometry);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => feature.SetWeight(weight));
        Assert.Contains("Weight must be between 0 and 1", exception.Message);
    }

    [Fact]
    public void UpdateWeight_WithValidWeight_SetsWeightCorrectly()
    {
        // Arrange
        var feature = new PropertyFeature(
            propertyId: 1,
            classificationType: ClassificationType.FoodPlot,
            geometry: _testGeometry);
        const float weight = 0.9f;

        // Act
        feature.UpdateWeight(weight);

        // Assert
        Assert.Equal(weight, feature.Weight);
    }

    [Fact]
    public void UpdateWeight_WithNull_SetsWeightToNull()
    {
        // Arrange
        var feature = new PropertyFeature(
            propertyId: 1,
            classificationType: ClassificationType.FoodPlot,
            geometry: _testGeometry,
            weight: 0.5f);

        // Act
        feature.UpdateWeight(null);

        // Assert
        Assert.Null(feature.Weight);
    }

    [Theory]
    [InlineData(-0.5f)]
    [InlineData(1.5f)]
    public void UpdateWeight_WithInvalidWeight_ThrowsArgumentOutOfRangeException(float weight)
    {
        // Arrange
        var feature = new PropertyFeature(
            propertyId: 1,
            classificationType: ClassificationType.FoodPlot,
            geometry: _testGeometry);

        // Act & Assert
        var exception = Assert.Throws<ArgumentOutOfRangeException>(() => feature.UpdateWeight(weight));
        Assert.Contains("Weight must be between 0 and 1", exception.Message);
    }

    [Fact]
    public void Constructor_AllParameters_SetsAllPropertiesCorrectly()
    {
        // Arrange
        const int propertyId = 123;
        const ClassificationType classificationType = ClassificationType.BeddingArea;
        const string name = "Test Bedding Area";
        const string notes = "Test notes";
        const float weight = 0.85f;
        const int createdBy = 456;
        var createdAt = DateTime.UtcNow;

        // Act
        var feature = new PropertyFeature(
            propertyId: propertyId,
            classificationType: classificationType,
            geometry: _testGeometry,
            name: name,
            notes: notes,
            weight: weight,
            createdBy: createdBy,
            createdAt: createdAt);

        // Assert
        Assert.Equal(propertyId, feature.PropertyId);
        Assert.Equal(classificationType, feature.ClassificationType);
        Assert.Equal(_testGeometry, feature.Geometry);
        Assert.Equal(name, feature.Name);
        Assert.Equal(notes, feature.Notes);
        Assert.Equal(weight, feature.Weight);
        Assert.Equal(createdBy, feature.CreatedBy);
        Assert.Equal(createdAt, feature.CreatedAt);
    }
}