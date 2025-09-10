using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using BuckScience.Domain.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.FeatureWeights;

public static class UpdateFeatureWeights
{
    public sealed record Command(
        Dictionary<ClassificationType, FeatureWeightUpdate> FeatureWeights);

    public sealed record FeatureWeightUpdate(
        float? UserWeight,
        Dictionary<Season, float>? SeasonalWeights,
        bool? ResetToDefault = null);

    public static async Task<bool> HandleAsync(
        Command command,
        IAppDbContext db,
        int propertyId,
        CancellationToken ct = default)
    {
        // Validate property exists
        var propertyExists = await db.Properties
            .AnyAsync(p => p.Id == propertyId, ct);

        if (!propertyExists)
            return false;

        // Get existing feature weights for the property
        var existingWeights = await db.FeatureWeights
            .Where(fw => fw.PropertyId == propertyId)
            .ToListAsync(ct);

        var existingWeightLookup = existingWeights.ToDictionary(fw => fw.ClassificationType, fw => fw);

        foreach (var kvp in command.FeatureWeights)
        {
            var classificationType = kvp.Key;
            var update = kvp.Value;

            // Validate weight values
            if (update.UserWeight.HasValue && (update.UserWeight.Value < 0 || update.UserWeight.Value > 1))
                throw new ArgumentException($"User weight for {classificationType} must be between 0 and 1");

            if (update.SeasonalWeights != null)
            {
                foreach (var seasonalWeight in update.SeasonalWeights.Values)
                {
                    if (seasonalWeight < 0 || seasonalWeight > 1)
                        throw new ArgumentException($"Seasonal weight for {classificationType} must be between 0 and 1");
                }
            }

            if (existingWeightLookup.TryGetValue(classificationType, out var existingWeight))
            {
                // Handle reset to default
                if (update.ResetToDefault == true)
                {
                    existingWeight.ResetToDefault();
                }
                else
                {
                    // Only update the properties that are actually being changed
                    // This prevents incorrectly setting IsCustom for unmodified properties
                    if (update.UserWeight.HasValue)
                    {
                        existingWeight.UpdateUserWeight(update.UserWeight);
                    }
                    
                    if (update.SeasonalWeights != null)
                    {
                        existingWeight.SetSeasonalWeights(update.SeasonalWeights);
                    }
                }
            }
            else
            {
                // This should not happen after materialization, but handle gracefully
                // Create new feature weight with proper IsCustom flag
                var defaultWeight = FeatureWeightHelper.GetDefaultWeight(classificationType);
                var hasCustomization = update.UserWeight.HasValue || update.SeasonalWeights != null;
                var newFeatureWeight = new FeatureWeight(
                    propertyId,
                    classificationType,
                    defaultWeight,
                    update.UserWeight,
                    update.SeasonalWeights,
                    hasCustomization);

                db.FeatureWeights.Add(newFeatureWeight);
            }
        }

        await db.SaveChangesAsync(ct);
        return true;
    }
}