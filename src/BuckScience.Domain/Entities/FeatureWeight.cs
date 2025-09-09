using BuckScience.Domain.Enums;
using System.Text.Json;

namespace BuckScience.Domain.Entities;

public class FeatureWeight
{
    protected FeatureWeight() { }

    public FeatureWeight(
        int propertyId,
        ClassificationType classificationType,
        float defaultWeight,
        float? userWeight = null,
        Dictionary<Season, float>? seasonalWeights = null)
    {
        PropertyId = propertyId;
        ClassificationType = classificationType;
        DefaultWeight = defaultWeight;
        UserWeight = userWeight;
        SetSeasonalWeights(seasonalWeights);
        UpdatedAt = DateTime.UtcNow;
    }

    public int Id { get; private set; }
    public int PropertyId { get; private set; }
    public ClassificationType ClassificationType { get; private set; }
    public float DefaultWeight { get; private set; }
    public float? UserWeight { get; private set; }
    public string? SeasonalWeightsJson { get; private set; }
    public DateTime UpdatedAt { get; private set; }

    // Navigation properties
    public virtual Property Property { get; private set; } = default!;

    public void UpdateUserWeight(float? userWeight)
    {
        UserWeight = userWeight;
        UpdatedAt = DateTime.UtcNow;
    }

    public void UpdateDefaultWeight(float defaultWeight)
    {
        if (defaultWeight < 0 || defaultWeight > 1)
            throw new ArgumentOutOfRangeException(nameof(defaultWeight), "Weight must be between 0 and 1");
        
        DefaultWeight = defaultWeight;
        UpdatedAt = DateTime.UtcNow;
    }

    public void SetSeasonalWeights(Dictionary<Season, float>? seasonalWeights)
    {
        if (seasonalWeights != null)
        {
            // Validate weights are in valid range
            foreach (var weight in seasonalWeights.Values)
            {
                if (weight < 0 || weight > 1)
                    throw new ArgumentOutOfRangeException(nameof(seasonalWeights), "All weights must be between 0 and 1");
            }
        }

        SeasonalWeightsJson = seasonalWeights != null ? JsonSerializer.Serialize(seasonalWeights) : null;
        UpdatedAt = DateTime.UtcNow;
    }

    public Dictionary<Season, float>? GetSeasonalWeights()
    {
        if (string.IsNullOrEmpty(SeasonalWeightsJson))
            return null;

        try
        {
            return JsonSerializer.Deserialize<Dictionary<Season, float>>(SeasonalWeightsJson);
        }
        catch (JsonException)
        {
            return null;
        }
    }

    public float GetEffectiveWeight(Season? currentSeason = null)
    {
        // If we have a current season and seasonal weights defined, use that
        if (currentSeason.HasValue)
        {
            var seasonalWeights = GetSeasonalWeights();
            if (seasonalWeights != null && seasonalWeights.ContainsKey(currentSeason.Value))
            {
                return seasonalWeights[currentSeason.Value];
            }
        }

        // Otherwise use user weight if set, or default weight
        return UserWeight ?? DefaultWeight;
    }
}