using BuckScience.Application.Abstractions;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.FeatureWeights;

public static class GetEffectiveFeatureWeight
{
    public static async Task<float> HandleAsync(
        IAppDbContext db,
        int propertyFeatureId,
        Season? currentSeason = null,
        CancellationToken ct = default)
    {
        // Get the PropertyFeature with its associated property data
        var feature = await db.PropertyFeatures
            .AsNoTracking()
            .Where(pf => pf.Id == propertyFeatureId)
            .Select(pf => new
            {
                pf.Weight,
                pf.ClassificationType,
                pf.PropertyId
            })
            .FirstOrDefaultAsync(ct);

        if (feature == null)
            throw new ArgumentException($"PropertyFeature with ID {propertyFeatureId} not found");

        // If the PropertyFeature has its own weight, use that (highest priority)
        if (feature.Weight.HasValue)
            return feature.Weight.Value;

        // Otherwise, get the property-level weight
        var featureWeight = await db.FeatureWeights
            .AsNoTracking()
            .FirstOrDefaultAsync(fw => fw.PropertyId == feature.PropertyId && 
                                      fw.ClassificationType == feature.ClassificationType, ct);

        if (featureWeight == null)
        {
            // This shouldn't happen after materialization, but fallback to system default
            return Domain.Helpers.FeatureWeightHelper.GetDefaultWeight(feature.ClassificationType);
        }

        // Use the property-level effective weight
        return featureWeight.GetEffectiveWeight(currentSeason);
    }

    /// <summary>
    /// Gets effective weights for all features of a property, considering individual feature weights
    /// </summary>
    public static async Task<Dictionary<int, float>> GetAllPropertyFeatureWeightsAsync(
        IAppDbContext db,
        int propertyId,
        Season? currentSeason = null,
        CancellationToken ct = default)
    {
        // Get all PropertyFeatures for the property
        var features = await db.PropertyFeatures
            .AsNoTracking()
            .Where(pf => pf.PropertyId == propertyId)
            .Select(pf => new
            {
                pf.Id,
                pf.Weight,
                pf.ClassificationType
            })
            .ToListAsync(ct);

        // Get property-level weights
        var propertyWeights = await db.FeatureWeights
            .AsNoTracking()
            .Where(fw => fw.PropertyId == propertyId)
            .ToListAsync(ct);

        var propertyWeightLookup = propertyWeights.ToDictionary(fw => fw.ClassificationType, fw => fw);

        var result = new Dictionary<int, float>();

        foreach (var feature in features)
        {
            // If the feature has its own weight, use that
            if (feature.Weight.HasValue)
            {
                result[feature.Id] = feature.Weight.Value;
                continue;
            }

            // Otherwise, use property-level weight
            if (propertyWeightLookup.TryGetValue(feature.ClassificationType, out var featureWeight))
            {
                result[feature.Id] = featureWeight.GetEffectiveWeight(currentSeason);
            }
            else
            {
                // Fallback to system default (shouldn't happen after materialization)
                result[feature.Id] = Domain.Helpers.FeatureWeightHelper.GetDefaultWeight(feature.ClassificationType);
            }
        }

        return result;
    }
}