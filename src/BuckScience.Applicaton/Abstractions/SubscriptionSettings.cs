using BuckScience.Domain.Enums;

namespace BuckScience.Application.Abstractions;

public class SubscriptionSettings
{
    public const string SectionName = "SubscriptionLimits";

    public int TrialDays { get; set; } = 14;
    
    public Dictionary<string, SubscriptionLimits> Tiers { get; set; } = new();

    public Dictionary<SubscriptionTier, SubscriptionLimits> GetLimitsByTier()
    {
        var result = new Dictionary<SubscriptionTier, SubscriptionLimits>();
        
        foreach (var (tierName, limits) in Tiers)
        {
            if (Enum.TryParse<SubscriptionTier>(tierName, true, out var tier))
            {
                result[tier] = limits;
            }
        }

        // Provide defaults if not configured
        if (!result.ContainsKey(SubscriptionTier.Trial))
            result[SubscriptionTier.Trial] = new SubscriptionLimits { MaxProperties = 1, MaxCameras = 2, MaxPhotos = 100 };
        if (!result.ContainsKey(SubscriptionTier.Fawn))
            result[SubscriptionTier.Fawn] = new SubscriptionLimits { MaxProperties = 3, MaxCameras = 6, MaxPhotos = 500 };
        if (!result.ContainsKey(SubscriptionTier.Doe))
            result[SubscriptionTier.Doe] = new SubscriptionLimits { MaxProperties = 5, MaxCameras = 15, MaxPhotos = 2000 };
        if (!result.ContainsKey(SubscriptionTier.Buck))
            result[SubscriptionTier.Buck] = new SubscriptionLimits { MaxProperties = 10, MaxCameras = 30, MaxPhotos = 10000 };
        if (!result.ContainsKey(SubscriptionTier.Trophy))
            result[SubscriptionTier.Trophy] = new SubscriptionLimits { MaxProperties = int.MaxValue, MaxCameras = int.MaxValue, MaxPhotos = int.MaxValue };
        if (!result.ContainsKey(SubscriptionTier.Expired))
            result[SubscriptionTier.Expired] = new SubscriptionLimits { MaxProperties = 0, MaxCameras = 0, MaxPhotos = 0 };

        return result;
    }
}

public class SubscriptionLimits
{
    public int MaxProperties { get; set; }
    public int MaxCameras { get; set; }
    public int MaxPhotos { get; set; }
}