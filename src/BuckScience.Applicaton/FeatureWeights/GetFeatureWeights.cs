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
        bool IsCustom,
        DateTime? UpdatedAt);

    public static async Task<IReadOnlyList<Result>> HandleAsync(
        IAppDbContext db,
        int propertyId,
        Season? currentSeason = null,
        CancellationToken ct = default)
    {
        // Get property's existing feature weights - all should be materialized
        var propertyWeights = await db.FeatureWeights
            .AsNoTracking()
            .Where(fw => fw.PropertyId == propertyId)
            .ToListAsync(ct);

        var results = new List<Result>();

        foreach (var featureWeight in propertyWeights)
        {
            var seasonalWeights = featureWeight.GetSeasonalWeights();
            var effectiveWeight = featureWeight.GetEffectiveWeight(currentSeason);
            var classificationName = FeatureWeightHelper.GetDisplayName(featureWeight.ClassificationType);
            var category = FeatureWeightHelper.GetCategory(featureWeight.ClassificationType);

            results.Add(new Result(
                featureWeight.ClassificationType,
                classificationName,
                category,
                featureWeight.DefaultWeight,
                featureWeight.UserWeight,
                seasonalWeights,
                effectiveWeight,
                featureWeight.IsCustom,
                featureWeight.UpdatedAt));
        }

        return results.OrderBy(r => r.Category).ThenBy(r => r.ClassificationName).ToList();
    }
}