using BuckScience.Application.Abstractions;
using BuckScience.Application.Services;
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
        DateTime? UpdatedAt,
        int IndividualFeatureCount,
        int IndividualFeatureOverrides);

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

        // Get feature count and override statistics for each classification type
        var featureStats = await db.PropertyFeatures
            .AsNoTracking()
            .Where(pf => pf.PropertyId == propertyId)
            .GroupBy(pf => pf.ClassificationType)
            .Select(g => new
            {
                ClassificationType = g.Key,
                TotalCount = g.Count(),
                OverrideCount = g.Count(pf => pf.Weight.HasValue)
            })
            .ToListAsync(ct);

        var featureStatsLookup = featureStats.ToDictionary(fs => fs.ClassificationType, fs => fs);

        var results = new List<Result>();

        foreach (var featureWeight in propertyWeights)
        {
            var seasonalWeights = featureWeight.GetSeasonalWeights();
            var effectiveWeight = featureWeight.GetEffectiveWeight(currentSeason);
            var classificationName = FeatureWeightHelper.GetDisplayName(featureWeight.ClassificationType);
            var category = FeatureWeightHelper.GetCategory(featureWeight.ClassificationType);

            // Get feature count and override statistics
            var stats = featureStatsLookup.GetValueOrDefault(featureWeight.ClassificationType);
            var featureCount = stats?.TotalCount ?? 0;
            var featureOverrides = stats?.OverrideCount ?? 0;

            results.Add(new Result(
                featureWeight.ClassificationType,
                classificationName,
                category,
                featureWeight.DefaultWeight,
                featureWeight.UserWeight,
                seasonalWeights,
                effectiveWeight,
                featureWeight.IsCustom,
                featureWeight.UpdatedAt,
                featureCount,
                featureOverrides));
        }

        return results.OrderBy(r => r.Category).ThenBy(r => r.ClassificationName).ToList();
    }

    /// <summary>
    /// Gets feature weights for a property with season determined from a date using hybrid season-month mapping.
    /// This method uses the SeasonMonthMappingService to resolve the active season from the given date and property,
    /// supporting both default season mappings and property-specific overrides.
    /// </summary>
    /// <param name="db">The database context.</param>
    /// <param name="seasonMappingService">The season month mapping service for hybrid season resolution.</param>
    /// <param name="propertyId">The ID of the property.</param>
    /// <param name="date">The date to determine the active season for.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>List of feature weight results for the property and resolved season.</returns>
    /// <exception cref="InvalidOperationException">Thrown when the property with the specified ID is not found.</exception>
    /// <remarks>
    /// <para>This method implements the hybrid season-month mapping logic:</para>
    /// <list type="number">
    /// <item>Checks for property-specific season overrides in the database</item>
    /// <item>Falls back to default season mappings from MonthsAttribute if no override exists</item>
    /// <item>Handles edge cases such as overlapping seasons and missing data</item>
    /// <item>Returns the first matching season by enum order for effective weight calculation</item>
    /// </list>
    /// <para>Supports all user types: hunters (default mappings), land managers (selective overrides), 
    /// and researchers (extensive customization).</para>
    /// </remarks>
    public static async Task<IReadOnlyList<Result>> HandleAsync(
        IAppDbContext db,
        SeasonMonthMappingService seasonMappingService,
        int propertyId,
        DateTime date,
        CancellationToken ct = default)
    {
        // Get the property to use for season resolution
        var property = await db.Properties
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == propertyId, ct);

        if (property == null)
        {
            throw new InvalidOperationException($"Property with ID {propertyId} not found.");
        }

        // Use hybrid season-month mapping to determine the active season for the date
        var activeSeason = await seasonMappingService.GetPrimarySeasonForDateAsync(date, property, ct);

        // Use the existing overload with the resolved season
        return await HandleAsync(db, propertyId, activeSeason, ct);
    }
}