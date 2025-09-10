using BuckScience.Application.FeatureWeights;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using BuckScience.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using Xunit;

namespace BuckScience.Tests.Application.FeatureWeights;

public class UpdateFeatureWeightsTests : IDisposable
{
    private readonly AppDbContext _context;
    private readonly GeometryFactory _geometryFactory;
    private readonly Property _testProperty;

    public UpdateFeatureWeightsTests()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new AppDbContext(options);
        _geometryFactory = new GeometryFactory();

        // Setup test data
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
        _testProperty = new Property(
            "Test Property",
            location,
            null, // boundary
            "America/Chicago",
            6, // dayHour
            18 // nightHour
        );
        _testProperty.AssignOwner(user.Id);
        
        _context.ApplicationUsers.Add(user);
        _context.Properties.Add(_testProperty);
        _context.SaveChanges();
    }

    [Fact]
    public async Task UpdateFeatureWeights_OnlyUpdatesSpecificWeight_SetsOnlyThatAsCustom()
    {
        // Arrange
        var beddingArea = new FeatureWeight(
            _testProperty.Id,
            ClassificationType.BeddingArea,
            0.9f, // default
            userWeight: null,
            seasonalWeights: null,
            isCustom: false);

        var cropField = new FeatureWeight(
            _testProperty.Id,
            ClassificationType.AgCropField,
            0.8f, // default
            userWeight: null,
            seasonalWeights: null,
            isCustom: false);

        _context.FeatureWeights.AddRange(beddingArea, cropField);
        await _context.SaveChangesAsync();

        // Only update the bedding area user weight
        var featureWeights = new Dictionary<ClassificationType, UpdateFeatureWeights.FeatureWeightUpdate>
        {
            { 
                ClassificationType.BeddingArea, 
                new UpdateFeatureWeights.FeatureWeightUpdate(
                    UserWeight: 0.95f, 
                    SeasonalWeights: null, 
                    ResetToDefault: null)
            }
        };

        var command = new UpdateFeatureWeights.Command(featureWeights);

        // Act
        var result = await UpdateFeatureWeights.HandleAsync(command, _context, _testProperty.Id, CancellationToken.None);

        // Assert
        Assert.True(result);

        var updatedBeddingArea = await _context.FeatureWeights
            .FirstAsync(fw => fw.PropertyId == _testProperty.Id && fw.ClassificationType == ClassificationType.BeddingArea);
        
        var unchangedCropField = await _context.FeatureWeights
            .FirstAsync(fw => fw.PropertyId == _testProperty.Id && fw.ClassificationType == ClassificationType.AgCropField);

        // Only the bedding area should be marked as custom
        Assert.True(updatedBeddingArea.IsCustom);
        Assert.Equal(0.95f, updatedBeddingArea.UserWeight);

        // The crop field should remain unchanged and not be marked as custom
        Assert.False(unchangedCropField.IsCustom);
        Assert.Null(unchangedCropField.UserWeight);
    }

    [Fact]
    public async Task UpdateFeatureWeights_OnlyUpdatesSeasonalWeights_SetsOnlyThatAsCustom()
    {
        // Arrange
        var beddingArea = new FeatureWeight(
            _testProperty.Id,
            ClassificationType.BeddingArea,
            0.9f, // default
            userWeight: null,
            seasonalWeights: null,
            isCustom: false);

        var cropField = new FeatureWeight(
            _testProperty.Id,
            ClassificationType.AgCropField,
            0.8f, // default
            userWeight: null,
            seasonalWeights: null,
            isCustom: false);

        _context.FeatureWeights.AddRange(beddingArea, cropField);
        await _context.SaveChangesAsync();

        // Only update the bedding area seasonal weights
        var seasonalWeights = new Dictionary<Season, float>
        {
            { Season.PreRut, 0.85f },
            { Season.Rut, 0.95f }
        };

        var featureWeights = new Dictionary<ClassificationType, UpdateFeatureWeights.FeatureWeightUpdate>
        {
            { 
                ClassificationType.BeddingArea, 
                new UpdateFeatureWeights.FeatureWeightUpdate(
                    UserWeight: null, 
                    SeasonalWeights: seasonalWeights, 
                    ResetToDefault: null)
            }
        };

        var command = new UpdateFeatureWeights.Command(featureWeights);

        // Act
        var result = await UpdateFeatureWeights.HandleAsync(command, _context, _testProperty.Id, CancellationToken.None);

        // Assert
        Assert.True(result);

        var updatedBeddingArea = await _context.FeatureWeights
            .FirstAsync(fw => fw.PropertyId == _testProperty.Id && fw.ClassificationType == ClassificationType.BeddingArea);
        
        var unchangedCropField = await _context.FeatureWeights
            .FirstAsync(fw => fw.PropertyId == _testProperty.Id && fw.ClassificationType == ClassificationType.AgCropField);

        // Only the bedding area should be marked as custom
        Assert.True(updatedBeddingArea.IsCustom);
        Assert.NotNull(updatedBeddingArea.GetSeasonalWeights());
        Assert.Equal(0.85f, updatedBeddingArea.GetSeasonalWeights()![Season.PreRut]);

        // The crop field should remain unchanged and not be marked as custom
        Assert.False(unchangedCropField.IsCustom);
        Assert.Null(unchangedCropField.GetSeasonalWeights());
    }

    [Fact]
    public async Task UpdateFeatureWeights_MultipleUpdates_SetsEachCorrectly()
    {
        // Arrange
        var beddingArea = new FeatureWeight(
            _testProperty.Id,
            ClassificationType.BeddingArea,
            0.9f,
            userWeight: null,
            seasonalWeights: null,
            isCustom: false);

        var cropField = new FeatureWeight(
            _testProperty.Id,
            ClassificationType.AgCropField,
            0.8f,
            userWeight: null,
            seasonalWeights: null,
            isCustom: false);

        var waterhole = new FeatureWeight(
            _testProperty.Id,
            ClassificationType.Waterhole,
            0.8f,
            userWeight: null,
            seasonalWeights: null,
            isCustom: false);

        _context.FeatureWeights.AddRange(beddingArea, cropField, waterhole);
        await _context.SaveChangesAsync();

        // Update two different weights in different ways
        var featureWeights = new Dictionary<ClassificationType, UpdateFeatureWeights.FeatureWeightUpdate>
        {
            { 
                ClassificationType.BeddingArea, 
                new UpdateFeatureWeights.FeatureWeightUpdate(
                    UserWeight: 0.95f,  // Update user weight only
                    SeasonalWeights: null, 
                    ResetToDefault: null)
            },
            { 
                ClassificationType.AgCropField, 
                new UpdateFeatureWeights.FeatureWeightUpdate(
                    UserWeight: null, 
                    SeasonalWeights: new Dictionary<Season, float> { { Season.PreRut, 0.75f } }, // Update seasonal weights only
                    ResetToDefault: null)
            }
            // Waterhole is not updated at all
        };

        var command = new UpdateFeatureWeights.Command(featureWeights);

        // Act
        var result = await UpdateFeatureWeights.HandleAsync(command, _context, _testProperty.Id, CancellationToken.None);

        // Assert
        Assert.True(result);

        var updatedBeddingArea = await _context.FeatureWeights
            .FirstAsync(fw => fw.PropertyId == _testProperty.Id && fw.ClassificationType == ClassificationType.BeddingArea);
        
        var updatedCropField = await _context.FeatureWeights
            .FirstAsync(fw => fw.PropertyId == _testProperty.Id && fw.ClassificationType == ClassificationType.AgCropField);

        var unchangedWaterhole = await _context.FeatureWeights
            .FirstAsync(fw => fw.PropertyId == _testProperty.Id && fw.ClassificationType == ClassificationType.Waterhole);

        // Both updated weights should be marked as custom
        Assert.True(updatedBeddingArea.IsCustom);
        Assert.Equal(0.95f, updatedBeddingArea.UserWeight);

        Assert.True(updatedCropField.IsCustom);
        Assert.NotNull(updatedCropField.GetSeasonalWeights());
        Assert.Equal(0.75f, updatedCropField.GetSeasonalWeights()![Season.PreRut]);

        // The waterhole should remain unchanged and not be marked as custom
        Assert.False(unchangedWaterhole.IsCustom);
        Assert.Null(unchangedWaterhole.UserWeight);
        Assert.Null(unchangedWaterhole.GetSeasonalWeights());
    }

    [Fact]
    public async Task UpdateFeatureWeights_ResetToDefault_ClearsAllCustomizations()
    {
        // Arrange
        var beddingArea = new FeatureWeight(
            _testProperty.Id,
            ClassificationType.BeddingArea,
            0.9f,
            userWeight: 0.95f, // has custom user weight
            seasonalWeights: new Dictionary<Season, float> { { Season.PreRut, 0.85f } }, // has custom seasonal weights
            isCustom: true);

        _context.FeatureWeights.Add(beddingArea);
        await _context.SaveChangesAsync();

        // Reset to default
        var featureWeights = new Dictionary<ClassificationType, UpdateFeatureWeights.FeatureWeightUpdate>
        {
            { 
                ClassificationType.BeddingArea, 
                new UpdateFeatureWeights.FeatureWeightUpdate(
                    UserWeight: null, 
                    SeasonalWeights: null, 
                    ResetToDefault: true)
            }
        };

        var command = new UpdateFeatureWeights.Command(featureWeights);

        // Act
        var result = await UpdateFeatureWeights.HandleAsync(command, _context, _testProperty.Id, CancellationToken.None);

        // Assert
        Assert.True(result);

        var updatedBeddingArea = await _context.FeatureWeights
            .FirstAsync(fw => fw.PropertyId == _testProperty.Id && fw.ClassificationType == ClassificationType.BeddingArea);

        // Should be reset to defaults
        Assert.False(updatedBeddingArea.IsCustom);
        Assert.Null(updatedBeddingArea.UserWeight);
        Assert.Null(updatedBeddingArea.GetSeasonalWeights());
    }

    [Fact]
    public async Task UpdateFeatureWeights_PartialUpdate_PreservesOtherCustomizations()
    {
        // Arrange - Start with both user weight and seasonal weights customized
        var seasonalWeights = new Dictionary<Season, float> { { Season.PreRut, 0.85f } };
        var beddingArea = new FeatureWeight(
            _testProperty.Id,
            ClassificationType.BeddingArea,
            0.9f,
            userWeight: 0.95f, // has custom user weight
            seasonalWeights: seasonalWeights, // has custom seasonal weights
            isCustom: true);

        _context.FeatureWeights.Add(beddingArea);
        await _context.SaveChangesAsync();

        // Update only the seasonal weights, don't touch user weight
        var newSeasonalWeights = new Dictionary<Season, float> 
        { 
            { Season.PreRut, 0.80f },
            { Season.Rut, 0.90f }
        };

        var featureWeights = new Dictionary<ClassificationType, UpdateFeatureWeights.FeatureWeightUpdate>
        {
            { 
                ClassificationType.BeddingArea, 
                new UpdateFeatureWeights.FeatureWeightUpdate(
                    UserWeight: null,  // Don't update this
                    SeasonalWeights: newSeasonalWeights,  // Update this
                    ResetToDefault: null)
            }
        };

        var command = new UpdateFeatureWeights.Command(featureWeights);

        // Act
        var result = await UpdateFeatureWeights.HandleAsync(command, _context, _testProperty.Id, CancellationToken.None);

        // Assert
        Assert.True(result);

        var updatedBeddingArea = await _context.FeatureWeights
            .FirstAsync(fw => fw.PropertyId == _testProperty.Id && fw.ClassificationType == ClassificationType.BeddingArea);

        // Should preserve the existing user weight and update seasonal weights
        Assert.True(updatedBeddingArea.IsCustom);
        Assert.Equal(0.95f, updatedBeddingArea.UserWeight); // Preserved
        
        var updatedSeasonalWeights = updatedBeddingArea.GetSeasonalWeights()!;
        Assert.Equal(0.80f, updatedSeasonalWeights[Season.PreRut]); // Updated
        Assert.Equal(0.90f, updatedSeasonalWeights[Season.Rut]); // New
    }

    [Fact]
    public async Task UpdateFeatureWeights_ClearUserWeight_KeepsCustomIfSeasonalWeightsExist()
    {
        // Arrange - Start with both user weight and seasonal weights customized
        var seasonalWeights = new Dictionary<Season, float> { { Season.PreRut, 0.85f } };
        var beddingArea = new FeatureWeight(
            _testProperty.Id,
            ClassificationType.BeddingArea,
            0.9f,
            userWeight: 0.95f, // has custom user weight
            seasonalWeights: seasonalWeights, // has custom seasonal weights
            isCustom: true);

        _context.FeatureWeights.Add(beddingArea);
        await _context.SaveChangesAsync();

        // Test the entity method directly - clear user weight while seasonal weights exist
        beddingArea.UpdateUserWeight(null);

        // Save changes
        await _context.SaveChangesAsync();

        // Assert
        var updatedBeddingArea = await _context.FeatureWeights
            .FirstAsync(fw => fw.PropertyId == _testProperty.Id && fw.ClassificationType == ClassificationType.BeddingArea);

        // Should still be custom because seasonal weights exist
        Assert.True(updatedBeddingArea.IsCustom);
        Assert.Null(updatedBeddingArea.UserWeight); // Cleared
        
        var existingSeasonalWeights = updatedBeddingArea.GetSeasonalWeights()!;
        Assert.Equal(0.85f, existingSeasonalWeights[Season.PreRut]); // Preserved
    }

    [Fact]
    public async Task UpdateFeatureWeights_SendingUnchangedValues_DoesNotTriggerCustomFlag()
    {
        // Arrange
        var beddingArea = new FeatureWeight(
            _testProperty.Id,
            ClassificationType.BeddingArea,
            0.9f, // default
            userWeight: null, // no custom user weight
            seasonalWeights: null, // no custom seasonal weights
            isCustom: false);

        var cropField = new FeatureWeight(
            _testProperty.Id,
            ClassificationType.AgCropField,
            0.8f, // default
            userWeight: null,
            seasonalWeights: null,
            isCustom: false);

        _context.FeatureWeights.AddRange(beddingArea, cropField);
        await _context.SaveChangesAsync();

        // Simulate what might happen if frontend sends all feature weights
        // but the user only actually changed one of them
        var featureWeights = new Dictionary<ClassificationType, UpdateFeatureWeights.FeatureWeightUpdate>
        {
            { 
                ClassificationType.BeddingArea, 
                new UpdateFeatureWeights.FeatureWeightUpdate(
                    UserWeight: 0.95f, // Actually changed by user
                    SeasonalWeights: null, 
                    ResetToDefault: null)
            },
            { 
                ClassificationType.AgCropField, 
                new UpdateFeatureWeights.FeatureWeightUpdate(
                    UserWeight: null, // Frontend sends current value (null), but it's unchanged
                    SeasonalWeights: null, // Frontend sends current value (null), but it's unchanged
                    ResetToDefault: null)
            }
        };

        var command = new UpdateFeatureWeights.Command(featureWeights);

        // Act
        var result = await UpdateFeatureWeights.HandleAsync(command, _context, _testProperty.Id, CancellationToken.None);

        // Assert
        Assert.True(result);

        var updatedBeddingArea = await _context.FeatureWeights
            .FirstAsync(fw => fw.PropertyId == _testProperty.Id && fw.ClassificationType == ClassificationType.BeddingArea);
        
        var unchangedCropField = await _context.FeatureWeights
            .FirstAsync(fw => fw.PropertyId == _testProperty.Id && fw.ClassificationType == ClassificationType.AgCropField);

        // Only the bedding area should be marked as custom (it was actually changed)
        Assert.True(updatedBeddingArea.IsCustom);
        Assert.Equal(0.95f, updatedBeddingArea.UserWeight);

        // The crop field should remain unchanged and not be marked as custom
        Assert.False(unchangedCropField.IsCustom);
        Assert.Null(unchangedCropField.UserWeight);
    }

    [Fact]
    public async Task UpdateFeatureWeights_SendingCurrentUserWeightValue_DoesNotTriggerCustomFlag()
    {
        // This test addresses the specific issue the user mentioned:
        // "when we initially add weights to the FeatureWeights table (upon property creation) 
        // the UserWeight is populated with the default value"
        
        // Arrange - Simulate the problematic scenario where UserWeight already equals DefaultWeight
        var beddingArea = new FeatureWeight(
            _testProperty.Id,
            ClassificationType.BeddingArea,
            0.9f, // default
            userWeight: 0.9f, // UserWeight populated with DefaultWeight during creation (the problem scenario)
            seasonalWeights: null,
            isCustom: false); // Should be false since it's not actually custom

        _context.FeatureWeights.Add(beddingArea);
        await _context.SaveChangesAsync();

        // Frontend sends an update request with the current UserWeight value (which equals DefaultWeight)
        var featureWeights = new Dictionary<ClassificationType, UpdateFeatureWeights.FeatureWeightUpdate>
        {
            { 
                ClassificationType.BeddingArea, 
                new UpdateFeatureWeights.FeatureWeightUpdate(
                    UserWeight: 0.9f, // Same as current UserWeight, which equals DefaultWeight
                    SeasonalWeights: null,
                    ResetToDefault: null)
            }
        };

        var command = new UpdateFeatureWeights.Command(featureWeights);

        // Act
        var result = await UpdateFeatureWeights.HandleAsync(command, _context, _testProperty.Id, CancellationToken.None);

        // Assert
        Assert.True(result);

        var updatedBeddingArea = await _context.FeatureWeights
            .FirstAsync(fw => fw.PropertyId == _testProperty.Id && fw.ClassificationType == ClassificationType.BeddingArea);

        // Should NOT be marked as custom since UserWeight equals DefaultWeight
        Assert.False(updatedBeddingArea.IsCustom);
        Assert.Equal(0.9f, updatedBeddingArea.UserWeight);
    }

    public void Dispose()
    {
        _context.Dispose();
    }
}