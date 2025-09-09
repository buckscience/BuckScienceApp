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
        int propertyId,
        Season? currentSeason = null,
        CancellationToken ct = default)
    {
        // Get property's existing feature weights
        var propertyWeights = await db.FeatureWeights
            .AsNoTracking()
            .Where(fw => fw.PropertyId == propertyId)
            .ToListAsync(ct);

        var propertyWeightLookup = propertyWeights.ToDictionary(fw => fw.ClassificationType, fw => fw);

        // Get all available classification types with default weights
        var allClassificationTypes = Enum.GetValues<ClassificationType>()
            .Where(ct => ct != ClassificationType.Other)
            .ToList();

        var results = new List<Result>();

        foreach (var classificationType in allClassificationTypes)
        {
            var defaultWeight = FeatureWeightHelper.GetDefaultWeight(classificationType);
            var propertyWeight = propertyWeightLookup.TryGetValue(classificationType, out var fw) ? fw : null;
            
            var seasonalWeights = propertyWeight?.GetSeasonalWeights();
            var effectiveWeight = propertyWeight?.GetEffectiveWeight(currentSeason) ?? defaultWeight;
            var classificationName = FeatureWeightHelper.GetDisplayName(classificationType);
            var category = FeatureWeightHelper.GetCategory(classificationType);

            results.Add(new Result(
                classificationType,
                classificationName,
                category,
                defaultWeight,
                propertyWeight?.UserWeight,
                seasonalWeights,
                effectiveWeight,
                propertyWeight?.UpdatedAt));
        }

        return results.OrderBy(r => r.Category).ThenBy(r => r.ClassificationName).ToList();
    }
}