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
        
        // Validate Stripe configuration before setting API key
        ValidateStripeConfiguration();
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
            // Use a timeout to prevent infinite hangs during debug
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(2));
            
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
                BillingAddressCollection = "required",
                // Configure subscription settings for auto-renewal
                SubscriptionData = new SessionSubscriptionDataOptions
                {
                    // Ensure the subscription is set up for automatic renewal
                    TrialPeriodDays = null, // No trial period for paid subscriptions
                    Metadata = new Dictionary<string, string>
                    {
                        { "user_tier", tier.ToString() },
                        { "created_from", "application_upgrade" }
                    }
                }
            };

            var service = new SessionService();
            var session = await service.CreateAsync(options, cancellationToken: cts.Token);
            
            _logger.LogInformation("Successfully created checkout session {SessionId} for customer {CustomerId} with tier {Tier} and price {PriceId}. Checkout URL: {CheckoutUrl}", 
                session.Id, customerId, tier, priceId, session.Url);
            return session.Url;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Timeout creating checkout session for customer {CustomerId} with tier {Tier} and price {PriceId}", 
                customerId, tier, priceId);
            throw new InvalidOperationException($"Request timed out while creating checkout session for {tier} tier. Please try again.");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating checkout session for customer {CustomerId} with tier {Tier} and price {PriceId}. StripeError: {StripeErrorType} - {StripeErrorCode} - {StripeErrorMessage}", 
                customerId, tier, priceId, ex.StripeError?.Type, ex.StripeError?.Code, ex.StripeError?.Message);
            throw new InvalidOperationException($"Failed to create checkout session for {tier} tier: {ex.Message} (Error Code: {ex.StripeError?.Code})", ex);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error creating checkout session for customer {CustomerId} with tier {Tier} and price {PriceId}: {ErrorMessage}", 
                customerId, tier, priceId, ex.Message);
            throw new InvalidOperationException($"An unexpected error occurred while creating checkout session for {tier} tier. Please try again or contact support.", ex);
        }
    }

    public async Task<string> UpdateSubscriptionAsync(string subscriptionId, SubscriptionTier newTier)
    {
        _logger.LogInformation("Updating Stripe subscription {SubscriptionId} to tier {NewTier}", subscriptionId, newTier);
        
        var priceId = GetPriceIdForTier(newTier);
        if (string.IsNullOrEmpty(priceId) || priceId.Contains("placeholder"))
        {
            _logger.LogError("No valid price ID configured for tier {Tier}. Price ID: {PriceId}", newTier, priceId);
            throw new InvalidOperationException($"No valid price ID configured for tier: {newTier}");
        }

        // Validate the price exists and is active in Stripe before updating
        await ValidatePriceInStripeAsync(priceId, newTier);

        try
        {
            // Use timeout to prevent debug hangs
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            
            var service = new Stripe.SubscriptionService();
            var subscription = await service.GetAsync(subscriptionId, cancellationToken: cts.Token);
            
            if (subscription == null)
            {
                _logger.LogError("Stripe subscription {SubscriptionId} not found", subscriptionId);
                throw new InvalidOperationException($"Subscription {subscriptionId} not found in Stripe");
            }

            if (subscription.Status != "active")
            {
                _logger.LogError("Stripe subscription {SubscriptionId} is not active (status: {Status})", subscriptionId, subscription.Status);
                throw new InvalidOperationException($"Cannot update inactive subscription (status: {subscription.Status})");
            }

            var subscriptionItemService = new SubscriptionItemService();
            var subscriptionItem = subscription.Items.Data.FirstOrDefault();
            
            if (subscriptionItem == null)
            {
                _logger.LogError("No subscription items found for subscription {SubscriptionId}", subscriptionId);
                throw new InvalidOperationException($"No subscription items found for subscription {subscriptionId}");
            }

            var options = new SubscriptionItemUpdateOptions
            {
                Price = priceId,
            };

            var updatedItem = await subscriptionItemService.UpdateAsync(subscriptionItem.Id, options, cancellationToken: cts.Token);
            
            _logger.LogInformation("Successfully updated subscription {SubscriptionId} item {ItemId} to price {PriceId} for tier {Tier}", 
                subscriptionId, subscriptionItem.Id, priceId, newTier);
            
            return subscription.Id;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Timeout updating subscription {SubscriptionId} to tier {Tier} with price {PriceId}", 
                subscriptionId, newTier, priceId);
            throw new InvalidOperationException($"Request timed out while updating subscription to {newTier} tier. Please try again.");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error updating subscription {SubscriptionId} to tier {Tier} with price {PriceId}. StripeError: {StripeErrorType} - {StripeErrorCode} - {StripeErrorMessage}", 
                subscriptionId, newTier, priceId, ex.StripeError?.Type, ex.StripeError?.Code, ex.StripeError?.Message);
            throw new InvalidOperationException($"Failed to update subscription to {newTier} tier: {ex.Message} (Error Code: {ex.StripeError?.Code})", ex);
        }
    }

    public async Task<string> CreateCustomerAsync(string email, string? name = null)
    {
        _logger.LogInformation("Creating Stripe customer for email {Email} with name {Name}", email, name);
        
        try
        {
            // Use timeout to prevent debug hangs
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            
            var options = new CustomerCreateOptions
            {
                Email = email,
                Name = name,
            };

            var service = new CustomerService();
            var customer = await service.CreateAsync(options, cancellationToken: cts.Token);
            
            _logger.LogInformation("Successfully created Stripe customer {CustomerId} for email {Email}", customer.Id, email);
            return customer.Id;
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Timeout creating customer for email {Email}", email);
            throw new InvalidOperationException($"Request timed out while creating customer for {email}. Please try again.");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error creating customer for email {Email}. StripeError: {StripeErrorType} - {StripeErrorCode} - {StripeErrorMessage}", 
                email, ex.StripeError?.Type, ex.StripeError?.Code, ex.StripeError?.Message);
            throw new InvalidOperationException($"Failed to create Stripe customer for {email}: {ex.Message} (Error Code: {ex.StripeError?.Code})", ex);
        }
    }

    public async Task CancelSubscriptionAsync(string subscriptionId)
    {
        _logger.LogInformation("Canceling Stripe subscription {SubscriptionId}", subscriptionId);
        
        try
        {
            // Use timeout to prevent debug hangs
            using var cts = new CancellationTokenSource(TimeSpan.FromMinutes(1));
            
            var service = new Stripe.SubscriptionService();
            var canceledSubscription = await service.CancelAsync(subscriptionId, cancellationToken: cts.Token);
            
            _logger.LogInformation("Successfully canceled Stripe subscription {SubscriptionId}. Status: {Status}", 
                subscriptionId, canceledSubscription.Status);
        }
        catch (OperationCanceledException)
        {
            _logger.LogError("Timeout canceling subscription {SubscriptionId}", subscriptionId);
            throw new InvalidOperationException($"Request timed out while canceling subscription {subscriptionId}. Please try again.");
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error canceling subscription {SubscriptionId}. StripeError: {StripeErrorType} - {StripeErrorCode} - {StripeErrorMessage}", 
                subscriptionId, ex.StripeError?.Type, ex.StripeError?.Code, ex.StripeError?.Message);
            throw new InvalidOperationException($"Failed to cancel subscription {subscriptionId}: {ex.Message} (Error Code: {ex.StripeError?.Code})", ex);
        }
    }

    public async Task<Dictionary<SubscriptionTier, StripePriceInfo>> GetPricingInfoAsync()
    {
        _logger.LogInformation("Fetching pricing information from Stripe for all tiers");
        
        var priceIds = _stripeSettings.GetPriceIds();
        var pricingInfo = new Dictionary<SubscriptionTier, StripePriceInfo>();
        var priceService = new PriceService();
        var productService = new ProductService();

        foreach (var (tierName, priceId) in priceIds)
        {
            if (string.IsNullOrEmpty(priceId) || priceId.Contains("placeholder"))
            {
                _logger.LogWarning("Skipping tier {TierName} - no valid price ID configured (PriceId: {PriceId})", tierName, priceId);
                continue;
            }

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
                    
                    _logger.LogInformation("Successfully loaded pricing for tier {Tier}: {PriceId} - ${Amount} {Currency}/{Interval}", 
                        tier, price.Id, pricingInfo[tier].Amount, price.Currency, pricingInfo[tier].RecurringInterval);
                }
                else
                {
                    _logger.LogWarning("Could not parse tier name '{TierName}' to SubscriptionTier enum", tierName);
                }
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Stripe error fetching price {PriceId} for tier {TierName}. StripeError: {StripeErrorType} - {StripeErrorCode} - {StripeErrorMessage}", 
                    priceId, tierName, ex.StripeError?.Type, ex.StripeError?.Code, ex.StripeError?.Message);
                // Continue processing other prices - don't fail entirely
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error fetching pricing info for tier {TierName} with price ID {PriceId}", tierName, priceId);
                // Continue processing other prices - don't fail entirely
            }
        }

        _logger.LogInformation("Successfully loaded pricing information for {Count} tiers", pricingInfo.Count);
        return pricingInfo;
    }

    public async Task<StripePriceInfo?> GetPriceInfoAsync(SubscriptionTier tier)
    {
        _logger.LogInformation("Fetching price information for tier {Tier}", tier);
        
        var priceId = GetPriceIdForTier(tier);
        if (string.IsNullOrEmpty(priceId) || priceId.Contains("placeholder"))
        {
            _logger.LogWarning("No valid price ID configured for tier {Tier}. Price ID: {PriceId}", tier, priceId);
            return null;
        }

        try
        {
            var priceService = new PriceService();
            var productService = new ProductService();
            
            var price = await priceService.GetAsync(priceId);
            var product = await productService.GetAsync(price.ProductId);

            var priceInfo = new StripePriceInfo
            {
                PriceId = price.Id,
                Amount = price.UnitAmount.HasValue ? price.UnitAmount.Value / 100m : 0m, // Convert from cents
                Currency = price.Currency,
                RecurringInterval = price.Recurring?.Interval ?? "month",
                ProductName = product.Name,
                ProductDescription = product.Description,
                IsActive = price.Active && product.Active
            };
            
            _logger.LogInformation("Successfully loaded price info for tier {Tier}: {PriceId} - ${Amount} {Currency}/{Interval} (Active: {IsActive})", 
                tier, priceInfo.PriceId, priceInfo.Amount, priceInfo.Currency, priceInfo.RecurringInterval, priceInfo.IsActive);
            
            return priceInfo;
        }
        catch (StripeException ex)
        {
            _logger.LogError(ex, "Stripe error fetching price info for tier {Tier} with price ID {PriceId}. StripeError: {StripeErrorType} - {StripeErrorCode} - {StripeErrorMessage}", 
                tier, priceId, ex.StripeError?.Type, ex.StripeError?.Code, ex.StripeError?.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Unexpected error fetching price info for tier {Tier} with price ID {PriceId}", tier, priceId);
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

    /// <summary>
    /// Validates that Stripe configuration is properly set up before attempting API calls.
    /// This helps prevent application hangs due to invalid configuration.
    /// </summary>
    private void ValidateStripeConfiguration()
    {
        _logger.LogInformation("Validating Stripe configuration");
        
        var errors = new List<string>();

        // Validate required settings
        if (string.IsNullOrWhiteSpace(_stripeSettings.SecretKey))
            errors.Add("Stripe SecretKey is not configured");
        else if (!_stripeSettings.SecretKey.StartsWith("sk_"))
            errors.Add("Stripe SecretKey appears to be invalid (should start with 'sk_')");

        if (string.IsNullOrWhiteSpace(_stripeSettings.PublishableKey))
            errors.Add("Stripe PublishableKey is not configured");
        else if (!_stripeSettings.PublishableKey.StartsWith("pk_"))
            errors.Add("Stripe PublishableKey appears to be invalid (should start with 'pk_')");

        if (string.IsNullOrWhiteSpace(_stripeSettings.WebhookSecret))
            errors.Add("Stripe WebhookSecret is not configured");
        else if (!_stripeSettings.WebhookSecret.StartsWith("whsec_"))
            errors.Add("Stripe WebhookSecret appears to be invalid (should start with 'whsec_')");

        // Validate price IDs are configured
        var priceIds = _stripeSettings.GetPriceIds();
        var validPriceCount = 0;
        foreach (var (tierName, priceId) in priceIds)
        {
            if (!string.IsNullOrEmpty(priceId) && !priceId.Contains("placeholder"))
            {
                if (!priceId.StartsWith("price_"))
                    errors.Add($"Price ID for tier {tierName} appears to be invalid (should start with 'price_'): {priceId}");
                else
                    validPriceCount++;
            }
        }

        if (validPriceCount == 0)
            errors.Add("No valid price IDs are configured for any subscription tiers");

        if (errors.Any())
        {
            var errorMessage = "Stripe configuration validation failed:\n" + string.Join("\n", errors.Select(e => $"- {e}"));
            _logger.LogError("Stripe configuration validation failed: {Errors}", string.Join("; ", errors));
            throw new InvalidOperationException(errorMessage);
        }

        _logger.LogInformation("Stripe configuration validation successful. Found {ValidPriceCount} valid price configurations", validPriceCount);
    }
}