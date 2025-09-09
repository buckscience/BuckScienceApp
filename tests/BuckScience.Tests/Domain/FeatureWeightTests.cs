using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using BuckScience.Domain.Helpers;
using System.Text.Json;
using Xunit;

namespace BuckScience.Tests.Domain;

public class FeatureWeightTests
{
    [Fact]
    public void FeatureWeight_Constructor_SetsPropertiesCorrectly()
    {
        // Arrange
        var userId = 1;
        var classificationType = ClassificationType.BeddingArea;
        var defaultWeight = 0.8f;
        var userWeight = 0.9f;
        var seasonalWeights = new Dictionary<Season, float>
        {
            { Season.PreRut, 0.7f },
            { Season.Rut, 0.9f },
            { Season.PostRut, 0.6f }
        };

        // Act
        var featureWeight = new FeatureWeight(userId, classificationType, defaultWeight, userWeight, seasonalWeights);

        // Assert
        Assert.Equal(userId, featureWeight.ApplicationUserId);
        Assert.Equal(classificationType, featureWeight.ClassificationType);
        Assert.Equal(defaultWeight, featureWeight.DefaultWeight);
        Assert.Equal(userWeight, featureWeight.UserWeight);
        Assert.NotNull(featureWeight.SeasonalWeightsJson);
        
        var deserializedWeights = featureWeight.GetSeasonalWeights();
        Assert.NotNull(deserializedWeights);
        Assert.Equal(0.7f, deserializedWeights[Season.PreRut]);
        Assert.Equal(0.9f, deserializedWeights[Season.Rut]);
        Assert.Equal(0.6f, deserializedWeights[Season.PostRut]);
    }

    [Fact]
    public void FeatureWeight_GetEffectiveWeight_ReturnsSeasonalWeightWhenAvailable()
    {
        // Arrange
        var featureWeight = new FeatureWeight(1, ClassificationType.BeddingArea, 0.5f, 0.7f, new Dictionary<Season, float>
        {
            { Season.PreRut, 0.8f },
            { Season.Rut, 0.9f }
        });

        // Act & Assert
        Assert.Equal(0.8f, featureWeight.GetEffectiveWeight(Season.PreRut));
        Assert.Equal(0.9f, featureWeight.GetEffectiveWeight(Season.Rut));
        Assert.Equal(0.7f, featureWeight.GetEffectiveWeight(Season.PostRut)); // Falls back to user weight
        Assert.Equal(0.7f, featureWeight.GetEffectiveWeight(null)); // Falls back to user weight
    }

    [Fact]
    public void FeatureWeight_GetEffectiveWeight_FallsBackToDefaultWeight()
    {
        // Arrange
        var featureWeight = new FeatureWeight(1, ClassificationType.BeddingArea, 0.5f);

        // Act & Assert
        Assert.Equal(0.5f, featureWeight.GetEffectiveWeight(Season.PreRut));
        Assert.Equal(0.5f, featureWeight.GetEffectiveWeight(null));
    }

    [Fact]
    public void FeatureWeight_UpdateUserWeight_UpdatesValueAndTimestamp()
    {
        // Arrange
        var featureWeight = new FeatureWeight(1, ClassificationType.BeddingArea, 0.5f);
        var originalTimestamp = featureWeight.UpdatedAt;

        // Act
        Thread.Sleep(10); // Ensure timestamp difference
        featureWeight.UpdateUserWeight(0.8f);

        // Assert
        Assert.Equal(0.8f, featureWeight.UserWeight);
        Assert.True(featureWeight.UpdatedAt > originalTimestamp);
    }

    [Fact]
    public void FeatureWeight_SetSeasonalWeights_ValidatesWeightRange()
    {
        // Arrange
        var featureWeight = new FeatureWeight(1, ClassificationType.BeddingArea, 0.5f);
        var invalidWeights = new Dictionary<Season, float>
        {
            { Season.PreRut, 1.5f } // Invalid: > 1
        };

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => featureWeight.SetSeasonalWeights(invalidWeights));
    }

    [Fact]
    public void FeatureWeight_UpdateDefaultWeight_ValidatesWeightRange()
    {
        // Arrange
        var featureWeight = new FeatureWeight(1, ClassificationType.BeddingArea, 0.5f);

        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(() => featureWeight.UpdateDefaultWeight(-0.1f));
        Assert.Throws<ArgumentOutOfRangeException>(() => featureWeight.UpdateDefaultWeight(1.1f));
    }

    [Theory]
    [InlineData(ClassificationType.BeddingArea, 0.9f)]
    [InlineData(ClassificationType.PinchPointFunnel, 0.9f)]
    [InlineData(ClassificationType.AgCropField, 0.8f)]
    [InlineData(ClassificationType.Waterhole, 0.8f)]
    [InlineData(ClassificationType.Saddle, 0.8f)]
    public void FeatureWeightHelper_GetDefaultWeight_ReturnsExpectedValues(ClassificationType type, float expectedWeight)
    {
        // Act
        var actualWeight = FeatureWeightHelper.GetDefaultWeight(type);

        // Assert
        Assert.Equal(expectedWeight, actualWeight);
    }

    [Theory]
    [InlineData(ClassificationType.BeddingArea, FeatureCategory.ResourceBedding)]
    [InlineData(ClassificationType.AgCropField, FeatureCategory.ResourceFood)]
    [InlineData(ClassificationType.Creek, FeatureCategory.ResourceWater)]
    [InlineData(ClassificationType.Ridge, FeatureCategory.Topographical)]
    public void FeatureWeightHelper_GetCategory_ReturnsCorrectCategory(ClassificationType type, FeatureCategory expectedCategory)
    {
        // Act
        var actualCategory = FeatureWeightHelper.GetCategory(type);

        // Assert
        Assert.Equal(expectedCategory, actualCategory);
    }

    [Fact]
    public void FeatureWeightHelper_GetDisplayName_ReturnsReadableNames()
    {
        // Act & Assert
        Assert.Equal("Bedding Area", FeatureWeightHelper.GetDisplayName(ClassificationType.BeddingArea));
        Assert.Equal("Agricultural Crop Field", FeatureWeightHelper.GetDisplayName(ClassificationType.AgCropField));
        Assert.Equal("Pinch Point/Funnel", FeatureWeightHelper.GetDisplayName(ClassificationType.PinchPointFunnel));
    }

    [Fact]
    public void FeatureWeight_SeasonalWeights_JsonSerialization_WorksCorrectly()
    {
        // Arrange
        var originalWeights = new Dictionary<Season, float>
        {
            { Season.PreRut, 0.7f },
            { Season.Rut, 0.9f },
            { Season.PostRut, 0.5f }
        };
        var featureWeight = new FeatureWeight(1, ClassificationType.BeddingArea, 0.6f, null, originalWeights);

        // Act
        var deserializedWeights = featureWeight.GetSeasonalWeights();

        // Assert
        Assert.NotNull(deserializedWeights);
        Assert.Equal(3, deserializedWeights.Count);
        Assert.Equal(0.7f, deserializedWeights[Season.PreRut]);
        Assert.Equal(0.9f, deserializedWeights[Season.Rut]);
        Assert.Equal(0.5f, deserializedWeights[Season.PostRut]);
    }

    [Fact]
    public void FeatureWeight_NullSeasonalWeights_ReturnsNullFromGetter()
    {
        // Arrange
        var featureWeight = new FeatureWeight(1, ClassificationType.BeddingArea, 0.6f);

        // Act
        var weights = featureWeight.GetSeasonalWeights();

        // Assert
        Assert.Null(weights);
    }
}