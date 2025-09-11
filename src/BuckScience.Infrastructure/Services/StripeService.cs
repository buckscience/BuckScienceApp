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

    public async Task<Dictionary<SubscriptionTier, StripePriceInfo>> GetPricingInfoAsync()
    {
        var priceIds = _stripeSettings.GetPriceIds();
        var pricingInfo = new Dictionary<SubscriptionTier, StripePriceInfo>();
        var priceService = new PriceService();
        var productService = new ProductService();

        foreach (var (tierName, priceId) in priceIds)
        {
            if (string.IsNullOrEmpty(priceId) || priceId.Contains("placeholder"))
                continue;

            try
            {
                var price = await priceService.GetAsync(priceId);
                var product = await productService.GetAsync(price.ProductId);

                if (Enum.TryParse<SubscriptionTier>(tierName, ignoreCase: true, out var tier))
                {
                    pricingInfo[tier] = new StripePriceInfo
                    {
                        PriceId = price.Id,
                        Amount = price.UnitAmount.HasValue ? price.UnitAmount.Value / 100m : 0m, // Convert from cents
                        Currency = price.Currency,
                        RecurringInterval = price.Recurring?.Interval ?? "month",
                        ProductName = product.Name,
                        ProductDescription = product.Description,
                        IsActive = price.Active && product.Active
                    };
                }
            }
            catch (StripeException ex)
            {
                // Log error but continue processing other prices
                // In a real application, you'd want to log this properly
                Console.WriteLine($"Error fetching price {priceId} for tier {tierName}: {ex.Message}");
            }
        }

        return pricingInfo;
    }

    public async Task<StripePriceInfo?> GetPriceInfoAsync(SubscriptionTier tier)
    {
        var priceId = GetPriceIdForTier(tier);
        if (string.IsNullOrEmpty(priceId) || priceId.Contains("placeholder"))
            return null;

        try
        {
            var priceService = new PriceService();
            var productService = new ProductService();
            
            var price = await priceService.GetAsync(priceId);
            var product = await productService.GetAsync(price.ProductId);

            return new StripePriceInfo
            {
                PriceId = price.Id,
                Amount = price.UnitAmount.HasValue ? price.UnitAmount.Value / 100m : 0m, // Convert from cents
                Currency = price.Currency,
                RecurringInterval = price.Recurring?.Interval ?? "month",
                ProductName = product.Name,
                ProductDescription = product.Description,
                IsActive = price.Active && product.Active
            };
        }
        catch (StripeException)
        {
            return null;
        }
    }

    private string? GetPriceIdForTier(SubscriptionTier tier)
    {
        var priceIds = _stripeSettings.GetPriceIds();
        var tierName = tier.ToString().ToLower();
        return priceIds.TryGetValue(tierName, out var priceId) ? priceId : null;
    }
}