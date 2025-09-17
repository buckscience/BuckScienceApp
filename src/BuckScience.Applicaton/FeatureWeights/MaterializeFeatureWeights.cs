using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using BuckScience.Domain.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.FeatureWeights;

public static class MaterializeFeatureWeights
{
    /// <summary>
    /// Creates FeatureWeight rows for all classification types for a property with default weights
    /// </summary>
    public static async Task HandleAsync(
        int propertyId,
        IAppDbContext db,
        CancellationToken ct = default)
    {
        // Get all available classification types except 'Other'
        var allClassificationTypes = Enum.GetValues<ClassificationType>()
            .Where(ct => ct != ClassificationType.Other)
            .ToList();

        // Get existing feature weights for this property to avoid duplicates
        var existingTypes = await db.FeatureWeights
            .Where(fw => fw.PropertyId == propertyId)
            .Select(fw => fw.ClassificationType)
            .ToListAsync(ct);

        var existingTypesSet = existingTypes.ToHashSet();

        // Create FeatureWeight records for missing classification types
        var featureWeightsToAdd = new List<FeatureWeight>();
        
        foreach (var classificationType in allClassificationTypes)
        {
            if (!existingTypesSet.Contains(classificationType))
            {
                var defaultWeight = FeatureWeightHelper.GetDefaultWeight(classificationType);
                var featureWeight = new FeatureWeight(
                    propertyId: propertyId,
                    classificationType: classificationType,
                    defaultWeight: defaultWeight,
                    userWeight: null,
                    seasonalWeights: null,
                    isCustom: false);

                featureWeightsToAdd.Add(featureWeight);
            }
        }

        if (featureWeightsToAdd.Any())
        {
            db.FeatureWeights.AddRange(featureWeightsToAdd);
            await db.SaveChangesAsync(ct);
        }
    }

    /// <summary>
    /// Materializes feature weights for all existing properties that don't have complete sets
    /// </summary>
    public static async Task MaterializeForAllPropertiesAsync(
        IAppDbContext db,
        CancellationToken ct = default)
    {
        var allPropertyIds = await db.Properties
            .Select(p => p.Id)
            .ToListAsync(ct);

        foreach (var propertyId in allPropertyIds)
        {
            await HandleAsync(propertyId, db, ct);
        }
    }
}