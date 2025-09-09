using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using BuckScience.Domain.Helpers;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.FeatureWeights;

public static class GetFeatureWeights
{
    public sealed record Result(
        ClassificationType ClassificationType,
        string ClassificationName,
        FeatureCategory Category,
        float DefaultWeight,
        float? UserWeight,
        Dictionary<Season, float>? SeasonalWeights,
        float EffectiveWeight,
        DateTime? UpdatedAt);

    public static async Task<IReadOnlyList<Result>> HandleAsync(
        IAppDbContext db,
        int userId,
        Season? currentSeason = null,
        CancellationToken ct = default)
    {
        // Get user's existing feature weights
        var userWeights = await db.FeatureWeights
            .AsNoTracking()
            .Where(fw => fw.ApplicationUserId == userId)
            .ToListAsync(ct);

        var userWeightLookup = userWeights.ToDictionary(fw => fw.ClassificationType, fw => fw);

        // Get all available classification types with default weights
        var allClassificationTypes = Enum.GetValues<ClassificationType>()
            .Where(ct => ct != ClassificationType.Other)
            .ToList();

        var results = new List<Result>();

        foreach (var classificationType in allClassificationTypes)
        {
            var defaultWeight = FeatureWeightHelper.GetDefaultWeight(classificationType);
            var userWeight = userWeightLookup.TryGetValue(classificationType, out var fw) ? fw : null;
            
            var seasonalWeights = userWeight?.GetSeasonalWeights();
            var effectiveWeight = userWeight?.GetEffectiveWeight(currentSeason) ?? defaultWeight;
            var classificationName = FeatureWeightHelper.GetDisplayName(classificationType);
            var category = FeatureWeightHelper.GetCategory(classificationType);

            results.Add(new Result(
                classificationType,
                classificationName,
                category,
                defaultWeight,
                userWeight?.UserWeight,
                seasonalWeights,
                effectiveWeight,
                userWeight?.UpdatedAt));
        }

        return results.OrderBy(r => r.Category).ThenBy(r => r.ClassificationName).ToList();
    }
}