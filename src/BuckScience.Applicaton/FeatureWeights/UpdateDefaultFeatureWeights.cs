using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.FeatureWeights;

public static class UpdateDefaultFeatureWeights
{
    public sealed record Command(
        Dictionary<ClassificationType, float> DefaultWeights);

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

        foreach (var kvp in command.DefaultWeights)
        {
            var classificationType = kvp.Key;
            var defaultWeight = kvp.Value;

            // Validate weight values
            if (defaultWeight < 0 || defaultWeight > 1)
                throw new ArgumentException($"Default weight for {classificationType} must be between 0 and 1");

            if (existingWeightLookup.TryGetValue(classificationType, out var existingWeight))
            {
                // Update existing feature weight's default
                existingWeight.UpdateDefaultWeight(defaultWeight);
            }
            else
            {
                // This should not happen after materialization, but handle gracefully
                // Create new feature weight with the specified default
                var newFeatureWeight = new FeatureWeight(
                    propertyId,
                    classificationType,
                    defaultWeight,
                    userWeight: null,
                    seasonalWeights: null,
                    isCustom: false);

                db.FeatureWeights.Add(newFeatureWeight);
            }
        }

        await db.SaveChangesAsync(ct);
        return true;
    }
}