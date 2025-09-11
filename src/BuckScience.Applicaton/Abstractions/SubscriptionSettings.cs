using BuckScience.Domain.Enums;

namespace BuckScience.Application.Abstractions;

public class SubscriptionSettings
{
    public const string SectionName = "SubscriptionSettings";

    public int TrialDays { get; set; } = 14;
    
    public Dictionary<SubscriptionTier, SubscriptionLimits> Limits { get; set; } = new()
    {
        { SubscriptionTier.Trial, new SubscriptionLimits { MaxProperties = 1, MaxCameras = 2, MaxPhotos = 100 } },
        { SubscriptionTier.Fawn, new SubscriptionLimits { MaxProperties = 3, MaxCameras = 6, MaxPhotos = 500 } },
        { SubscriptionTier.Doe, new SubscriptionLimits { MaxProperties = 5, MaxCameras = 15, MaxPhotos = 2000 } },
        { SubscriptionTier.Buck, new SubscriptionLimits { MaxProperties = 10, MaxCameras = 30, MaxPhotos = 10000 } },
        { SubscriptionTier.Trophy, new SubscriptionLimits { MaxProperties = int.MaxValue, MaxCameras = int.MaxValue, MaxPhotos = int.MaxValue } },
        { SubscriptionTier.Expired, new SubscriptionLimits { MaxProperties = 0, MaxCameras = 0, MaxPhotos = 0 } }
    };
}

public class SubscriptionLimits
{
    public int MaxProperties { get; set; }
    public int MaxCameras { get; set; }
    public int MaxPhotos { get; set; }
}