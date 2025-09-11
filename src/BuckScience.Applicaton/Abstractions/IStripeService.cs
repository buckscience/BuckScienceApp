using BuckScience.Domain.Enums;

namespace BuckScience.Application.Abstractions;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(string customerId, SubscriptionTier tier, string successUrl, string cancelUrl);
    Task<string> UpdateSubscriptionAsync(string subscriptionId, SubscriptionTier newTier);
    Task<string> CreateCustomerAsync(string email, string? name = null);
    Task CancelSubscriptionAsync(string subscriptionId);
}