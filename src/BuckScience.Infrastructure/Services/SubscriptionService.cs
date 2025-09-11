using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace BuckScience.Infrastructure.Services;

public class SubscriptionService : ISubscriptionService
{
    private readonly IAppDbContext _context;
    private readonly IStripeService _stripeService;
    private readonly SubscriptionSettings _settings;
    private readonly Dictionary<SubscriptionTier, SubscriptionLimits> _limits;

    public SubscriptionService(
        IAppDbContext context,
        IStripeService stripeService,
        IOptions<SubscriptionSettings> settings)
    {
        _context = context;
        _stripeService = stripeService;
        _settings = settings.Value;
        _limits = _settings.GetLimitsByTier();
    }

    public async Task<bool> CanAddPropertyAsync(int userId)
    {
        var tier = await GetUserSubscriptionTierAsync(userId);
        var currentCount = await _context.Properties.CountAsync(p => p.ApplicationUserId == userId);
        var maxAllowed = GetMaxProperties(tier);
        
        return currentCount < maxAllowed;
    }

    public async Task<bool> CanAddCameraAsync(int userId)
    {
        var tier = await GetUserSubscriptionTierAsync(userId);
        var currentCount = await _context.Cameras
            .Join(_context.Properties, c => c.PropertyId, p => p.Id, (c, p) => new { Camera = c, Property = p })
            .CountAsync(cp => cp.Property.ApplicationUserId == userId);
        var maxAllowed = GetMaxCameras(tier);
        
        return currentCount < maxAllowed;
    }

    public async Task<bool> CanUploadPhotoAsync(int userId)
    {
        var tier = await GetUserSubscriptionTierAsync(userId);
        var currentCount = await _context.Photos
            .Join(_context.Cameras, ph => ph.CameraId, c => c.Id, (ph, c) => new { Photo = ph, Camera = c })
            .Join(_context.Properties, phc => phc.Camera.PropertyId, p => p.Id, (phc, p) => new { phc.Photo, Property = p })
            .CountAsync(pcp => pcp.Property.ApplicationUserId == userId);
        var maxAllowed = GetMaxPhotos(tier);
        
        return currentCount < maxAllowed;
    }

    public async Task<Subscription?> GetUserSubscriptionAsync(int userId)
    {
        return await _context.Subscriptions
            .Include(s => s.User)
            .FirstOrDefaultAsync(s => s.UserId == userId);
    }

    public async Task<bool> HasActiveSubscriptionAsync(int userId)
    {
        var subscription = await GetUserSubscriptionAsync(userId);
        if (subscription == null) return false;
        
        return subscription.Status == "active" && 
               (subscription.CurrentPeriodEnd == null || subscription.CurrentPeriodEnd > DateTime.UtcNow);
    }

    public async Task<string> CreateSubscriptionAsync(int userId, SubscriptionTier tier, string successUrl, string cancelUrl)
    {
        var user = await _context.ApplicationUsers.FindAsync(userId);
        if (user == null)
        {
            throw new ArgumentException("User not found", nameof(userId));
        }

        var subscription = await GetUserSubscriptionAsync(userId);
        string customerId;

        if (subscription?.StripeCustomerId != null)
        {
            customerId = subscription.StripeCustomerId;
        }
        else
        {
            customerId = await _stripeService.CreateCustomerAsync(user.Email, user.DisplayName);
            
            if (subscription == null)
            {
                subscription = new Subscription
                {
                    UserId = userId,
                    StripeCustomerId = customerId,
                    Tier = tier,
                    Status = "active",
                    CreatedAt = DateTime.UtcNow
                };
                _context.Subscriptions.Add(subscription);
            }
            else
            {
                subscription.StripeCustomerId = customerId;
                subscription.Tier = tier;
            }

            await _context.SaveChangesAsync();
        }

        return await _stripeService.CreateCheckoutSessionAsync(customerId, tier, successUrl, cancelUrl);
    }

    public async Task<string> UpdateSubscriptionAsync(int userId, SubscriptionTier newTier, string successUrl, string cancelUrl)
    {
        var subscription = await GetUserSubscriptionAsync(userId);
        if (subscription == null)
        {
            // Enhanced error message with debugging information
            var allSubscriptions = await _context.Subscriptions.ToListAsync();
            var subscriptionCount = allSubscriptions.Count;
            var userIds = string.Join(", ", allSubscriptions.Select(s => s.UserId));
            
            throw new InvalidOperationException($"No subscription found for user ID {userId}. Total subscriptions in database: {subscriptionCount}. User IDs found: [{userIds}]");
        }

        // If this is a trial user (no Stripe IDs), treat it as a new subscription creation
        if (subscription.StripeSubscriptionId == null || subscription.StripeCustomerId == null)
        {
            return await CreateSubscriptionAsync(userId, newTier, successUrl, cancelUrl);
        }

        // For subscription updates, we'll create a new checkout session
        // In a real implementation, you might want to handle proration differently
        return await _stripeService.CreateCheckoutSessionAsync(subscription.StripeCustomerId, newTier, successUrl, cancelUrl);
    }

    public async Task<SubscriptionTier> GetUserSubscriptionTierAsync(int userId)
    {
        var subscription = await GetUserSubscriptionAsync(userId);
        
        if (subscription == null)
        {
            // Check if user is still in trial period
            var user = await _context.ApplicationUsers.FindAsync(userId);
            if (user?.TrialStartDate != null)
            {
                var trialEnd = user.TrialStartDate.Value.AddDays(_settings.TrialDays);
                if (DateTime.UtcNow <= trialEnd)
                {
                    return SubscriptionTier.Trial;
                }
            }
            return SubscriptionTier.Expired;
        }

        if (subscription.Status != "active")
        {
            return SubscriptionTier.Expired;
        }

        // Check if subscription period has ended
        if (subscription.CurrentPeriodEnd != null && subscription.CurrentPeriodEnd < DateTime.UtcNow)
        {
            return SubscriptionTier.Expired;
        }

        return subscription.Tier;
    }

    public async Task<bool> IsTrialExpiredAsync(int userId)
    {
        var user = await _context.ApplicationUsers.FindAsync(userId);
        if (user?.TrialStartDate == null) return true;

        var trialEnd = user.TrialStartDate.Value.AddDays(_settings.TrialDays);
        return DateTime.UtcNow > trialEnd;
    }

    public async Task<int> GetTrialDaysRemainingAsync(int userId)
    {
        var user = await _context.ApplicationUsers.FindAsync(userId);
        if (user?.TrialStartDate == null) return 0;

        var trialEnd = user.TrialStartDate.Value.AddDays(_settings.TrialDays);
        var remaining = (trialEnd - DateTime.UtcNow).Days;
        return Math.Max(0, remaining);
    }

    public int GetMaxProperties(SubscriptionTier tier)
    {
        return _limits.TryGetValue(tier, out var limits) ? limits.MaxProperties : 0;
    }

    public int GetMaxCameras(SubscriptionTier tier)
    {
        return _limits.TryGetValue(tier, out var limits) ? limits.MaxCameras : 0;
    }

    public int GetMaxPhotos(SubscriptionTier tier)
    {
        return _limits.TryGetValue(tier, out var limits) ? limits.MaxPhotos : 0;
    }
}