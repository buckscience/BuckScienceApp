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
        Dictionary<Season, float>? SeasonalWeights);

    public static async Task<bool> HandleAsync(
        Command command,
        IAppDbContext db,
        int userId,
        CancellationToken ct = default)
    {
        // Validate user exists
        var userExists = await db.ApplicationUsers
            .AnyAsync(u => u.Id == userId, ct);

        if (!userExists)
            return false;

        // Get existing feature weights for the user
        var existingWeights = await db.FeatureWeights
            .Where(fw => fw.ApplicationUserId == userId)
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
                // Update existing feature weight
                existingWeight.UpdateUserWeight(update.UserWeight);
                existingWeight.SetSeasonalWeights(update.SeasonalWeights);
            }
            else
            {
                // Create new feature weight
                var defaultWeight = FeatureWeightHelper.GetDefaultWeight(classificationType);
                var newFeatureWeight = new FeatureWeight(
                    userId,
                    classificationType,
                    defaultWeight,
                    update.UserWeight,
                    update.SeasonalWeights);

                db.FeatureWeights.Add(newFeatureWeight);
            }
        }

        await db.SaveChangesAsync(ct);
        return true;
    }
}