using BuckScience.Application.Abstractions;
using BuckScience.Domain.Enums;
using BuckScience.Shared.Configuration;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace BuckScience.Infrastructure.Services;

public class StripeService : IStripeService
{
    private readonly StripeSettings _stripeSettings;

    public StripeService(IOptions<StripeSettings> stripeSettings)
    {
        _stripeSettings = stripeSettings.Value;
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    }

    public async Task<string> CreateCheckoutSessionAsync(string customerId, SubscriptionTier tier, string successUrl, string cancelUrl)
    {
        var priceId = GetPriceIdForTier(tier);
        if (string.IsNullOrEmpty(priceId))
        {
            throw new InvalidOperationException($"No price ID configured for tier: {tier}");
        }

        var options = new SessionCreateOptions
        {
            Customer = customerId,
            PaymentMethodTypes = new List<string> { "card" },
            LineItems = new List<SessionLineItemOptions>
            {
                new SessionLineItemOptions
                {
                    Price = priceId,
                    Quantity = 1,
                }
            },
            Mode = "subscription",
            SuccessUrl = successUrl,
            CancelUrl = cancelUrl,
        };

        var service = new SessionService();
        var session = await service.CreateAsync(options);
        return session.Url;
    }

    public async Task<string> UpdateSubscriptionAsync(string subscriptionId, SubscriptionTier newTier)
    {
        var priceId = GetPriceIdForTier(newTier);
        if (string.IsNullOrEmpty(priceId))
        {
            throw new InvalidOperationException($"No price ID configured for tier: {newTier}");
        }

        var service = new Stripe.SubscriptionService();
        var subscription = await service.GetAsync(subscriptionId);

        var subscriptionItemService = new SubscriptionItemService();
        var subscriptionItem = subscription.Items.Data[0];

        var options = new SubscriptionItemUpdateOptions
        {
            Price = priceId,
        };

        await subscriptionItemService.UpdateAsync(subscriptionItem.Id, options);
        return subscription.Id;
    }

    public async Task<string> CreateCustomerAsync(string email, string? name = null)
    {
        var options = new CustomerCreateOptions
        {
            Email = email,
            Name = name,
        };

        var service = new CustomerService();
        var customer = await service.CreateAsync(options);
        return customer.Id;
    }

    public async Task CancelSubscriptionAsync(string subscriptionId)
    {
        var service = new Stripe.SubscriptionService();
        await service.CancelAsync(subscriptionId);
    }

    private string? GetPriceIdForTier(SubscriptionTier tier)
    {
        var tierName = tier.ToString().ToLower();
        return _stripeSettings.PriceIds.TryGetValue(tierName, out var priceId) ? priceId : null;
    }
}