using BuckScience.Domain.Enums;

namespace BuckScience.Application.Abstractions.Services;

public interface IStripeService
{
    Task<string> CreateCheckoutSessionAsync(string customerId, SubscriptionTier tier, string successUrl, string cancelUrl);
    Task<string> UpdateSubscriptionAsync(string subscriptionId, SubscriptionTier newTier);
    Task<string> CreateCustomerAsync(string email, string? name = null);
    Task CancelSubscriptionAsync(string subscriptionId);
    Task<Dictionary<SubscriptionTier, StripePriceInfo>> GetPricingInfoAsync();
    Task<StripePriceInfo?> GetPriceInfoAsync(SubscriptionTier tier);
}

public class StripePriceInfo
{
    public string PriceId { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Currency { get; set; } = "usd";
    public string RecurringInterval { get; set; } = "month";
    public string ProductName { get; set; } = string.Empty;
    public string? ProductDescription { get; set; }
    public bool IsActive { get; set; } = true;
}