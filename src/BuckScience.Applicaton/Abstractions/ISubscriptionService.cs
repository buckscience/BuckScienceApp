using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;

namespace BuckScience.Application.Abstractions;

public interface ISubscriptionService
{
    Task<bool> CanAddPropertyAsync(int userId);
    Task<bool> CanAddCameraAsync(int userId);
    Task<bool> CanUploadPhotoAsync(int userId);
    Task<Subscription?> GetUserSubscriptionAsync(int userId);
    Task<bool> HasActiveSubscriptionAsync(int userId);
    Task<string> CreateSubscriptionAsync(int userId, SubscriptionTier tier, string successUrl, string cancelUrl);
    Task<string> UpdateSubscriptionAsync(int userId, SubscriptionTier newTier, string successUrl, string cancelUrl);
    Task<SubscriptionTier> GetUserSubscriptionTierAsync(int userId);
    Task<bool> IsTrialExpiredAsync(int userId);
    Task<int> GetTrialDaysRemainingAsync(int userId);
    int GetMaxProperties(SubscriptionTier tier);
    int GetMaxCameras(SubscriptionTier tier);
    int GetMaxPhotos(SubscriptionTier tier);
}