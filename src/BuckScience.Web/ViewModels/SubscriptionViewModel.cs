using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;

namespace BuckScience.Web.ViewModels;

public class SubscriptionViewModel
{
    public Subscription? Subscription { get; set; }
    public SubscriptionTier CurrentTier { get; set; } = SubscriptionTier.Trial;
    public int TrialDaysRemaining { get; set; }
    public bool IsTrialExpired { get; set; }
    public int MaxProperties { get; set; }
    public int MaxCameras { get; set; }
    public int MaxPhotos { get; set; }

    public string GetTierDisplayName()
    {
        return CurrentTier switch
        {
            SubscriptionTier.Trial => "Free Trial",
            SubscriptionTier.Fawn => "Fawn Plan",
            SubscriptionTier.Doe => "Doe Plan",
            SubscriptionTier.Buck => "Buck Plan",
            SubscriptionTier.Trophy => "Trophy Plan",
            SubscriptionTier.Expired => "Expired",
            _ => "Unknown"
        };
    }

    public string GetTierDescription()
    {
        return CurrentTier switch
        {
            SubscriptionTier.Trial => "Perfect for getting started",
            SubscriptionTier.Fawn => "Great for small properties",
            SubscriptionTier.Doe => "Ideal for medium properties",
            SubscriptionTier.Buck => "Perfect for large properties",
            SubscriptionTier.Trophy => "Unlimited for serious hunters",
            SubscriptionTier.Expired => "Your subscription has expired",
            _ => ""
        };
    }

    public decimal GetTierPrice()
    {
        return CurrentTier switch
        {
            SubscriptionTier.Trial => 0m,
            SubscriptionTier.Fawn => 9.99m,
            SubscriptionTier.Doe => 19.99m,
            SubscriptionTier.Buck => 39.99m,
            SubscriptionTier.Trophy => 79.99m,
            SubscriptionTier.Expired => 0m,
            _ => 0m
        };
    }

    public bool IsActive()
    {
        return Subscription?.Status == "active" ||
               (CurrentTier == SubscriptionTier.Trial && !IsTrialExpired);
    }

    public bool CanUpgrade()
    {
        return CurrentTier != SubscriptionTier.Trophy && IsActive();
    }

    public bool CanDowngrade()
    {
        return CurrentTier != SubscriptionTier.Trial && 
               CurrentTier != SubscriptionTier.Fawn && 
               IsActive();
    }

    public List<SubscriptionTier> GetAvailableUpgrades()
    {
        var upgrades = new List<SubscriptionTier>();
        
        switch (CurrentTier)
        {
            case SubscriptionTier.Trial:
                upgrades.AddRange([SubscriptionTier.Fawn, SubscriptionTier.Doe, SubscriptionTier.Buck, SubscriptionTier.Trophy]);
                break;
            case SubscriptionTier.Fawn:
                upgrades.AddRange([SubscriptionTier.Doe, SubscriptionTier.Buck, SubscriptionTier.Trophy]);
                break;
            case SubscriptionTier.Doe:
                upgrades.AddRange([SubscriptionTier.Buck, SubscriptionTier.Trophy]);
                break;
            case SubscriptionTier.Buck:
                upgrades.Add(SubscriptionTier.Trophy);
                break;
        }
        
        return upgrades;
    }

    public List<SubscriptionTier> GetAvailableDowngrades()
    {
        var downgrades = new List<SubscriptionTier>();
        
        switch (CurrentTier)
        {
            case SubscriptionTier.Trophy:
                downgrades.AddRange([SubscriptionTier.Buck, SubscriptionTier.Doe, SubscriptionTier.Fawn]);
                break;
            case SubscriptionTier.Buck:
                downgrades.AddRange([SubscriptionTier.Doe, SubscriptionTier.Fawn]);
                break;
            case SubscriptionTier.Doe:
                downgrades.Add(SubscriptionTier.Fawn);
                break;
        }
        
        return downgrades;
    }
}