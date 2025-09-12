using BuckScience.Application.Abstractions;
using BuckScience.Domain.Enums;
using BuckScience.Shared.Configuration;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;

namespace BuckScience.Infrastructure.Services;

public class StripeService : IStripeService
{
    private readonly StripeSettings _stripeSettings;
    private readonly ILogger<StripeService> _logger;

    public StripeService(IOptions<StripeSettings> stripeSettings, ILogger<StripeService> logger)
    {
        _stripeSettings = stripeSettings.Value;
        _logger = logger;
        StripeConfiguration.ApiKey = _stripeSettings.SecretKey;
    }

    public async Task<string> CreateCheckoutSessionAsync(string customerId, SubscriptionTier tier, string successUrl, string cancelUrl)
    {
        _logger.LogInformation("Creating checkout session for customer {CustomerId} with tier {Tier}", customerId, tier);
        
        var priceId = GetPriceIdForTier(tier);
        if (string.IsNullOrEmpty(priceId) || priceId.Contains("placeholder"))
        {
            _logger.LogError("No valid price ID configured for tier {Tier}. Price ID: {PriceId}", tier, priceId);
            throw new InvalidOperationException($"No valid price ID configured for tier: {tier}");
        }

        // Validate the price exists and is active in Stripe before creating session
        await ValidatePriceInStripeAsync(priceId, tier);

        try
        {
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
                // Add locale and additional configuration to prevent checkout issues
                Locale = "auto",
                AllowPromotionCodes = false,
                BillingAddressCollection = "required"
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options);
            
            _logger.LogInformation("Successfully created checkout session {SessionId} for customer {CustomerId} with tier {Tier} and price {PriceId}. Checkout URL: {CheckoutUrl}", 
                session.Id, customerId, tier, priceId, session.Url);
            return session.Url;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session for customer {CustomerId} with tier {Tier} and price {PriceId}. StripeError: {StripeErrorType} - {StripeErrorCode} - {StripeErrorMessage}", 
                customerId, tier, priceId, ex.StripeError?.Type, ex.StripeError?.Code, ex.StripeError?.Message);
            throw new InvalidOperationException($"Failed to create checkout session for {tier} tier: {ex.Message} (Error Code: {ex.StripeError?.Code})", ex);
        }
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

    private async Task ValidatePriceInStripeAsync(string priceId, SubscriptionTier tier)
    {
        try
        {
            var priceService = new PriceService();
            var price = await priceService.GetAsync(priceId);
            
            if (!price.Active)
            {
                _logger.LogError("Price {PriceId} for tier {Tier} is inactive in Stripe", priceId, tier);
                throw new InvalidOperationException($"The {tier} plan is currently unavailable - price is inactive");
            }

            // Also validate the associated product
            var productService = new ProductService();
            var product = await productService.GetAsync(price.ProductId);
            
            if (!product.Active)
            {
                _logger.LogError("Product {ProductId} for price {PriceId} and tier {Tier} is inactive in Stripe", product.Id, priceId, tier);
                throw new InvalidOperationException($"The {tier} plan is currently unavailable - product is inactive");
            }

            _logger.LogInformation("Successfully validated price {PriceId} for tier {Tier}. Product: {ProductName} ({ProductId}), Amount: {Amount} {Currency}", 
                priceId, tier, product.Name, product.Id, price.UnitAmount, price.Currency);
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Failed to validate price {PriceId} for tier {Tier} in Stripe. Error: {StripeErrorType} - {StripeErrorCode}", 
                priceId, tier, ex.StripeError?.Type, ex.StripeError?.Code);
            throw new InvalidOperationException($"Unable to validate {tier} plan pricing: {ex.Message}", ex);
        }
    }
}