using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Domain.Enums;
using BuckScience.Shared.Configuration;
using BuckScience.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Stripe;
using Stripe.Checkout;
using DomainSubscription = BuckScience.Domain.Entities.Subscription;

namespace BuckScience.Web.Controllers;

[Authorize]
public class SubscriptionController : Controller
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStripeService _stripeService;
    private readonly IAppDbContext _context;
    private readonly ILogger<SubscriptionController> _logger;
    private readonly StripeSettings _stripeSettings;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        ICurrentUserService currentUserService,
        IStripeService stripeService,
        IAppDbContext context,
        ILogger<SubscriptionController> logger,
        IOptions<StripeSettings> stripeSettings)
    {
        _subscriptionService = subscriptionService;
        _currentUserService = currentUserService;
        _stripeService = stripeService;
        _context = context;
        _logger = logger;
        _stripeSettings = stripeSettings.Value;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (_currentUserService.Id is null) return Forbid();

        var subscription = await _subscriptionService.GetUserSubscriptionAsync(_currentUserService.Id.Value);
        var tier = await _subscriptionService.GetUserSubscriptionTierAsync(_currentUserService.Id.Value);
        var trialDaysRemaining = await _subscriptionService.GetTrialDaysRemainingAsync(_currentUserService.Id.Value);
        var isTrialExpired = await _subscriptionService.IsTrialExpiredAsync(_currentUserService.Id.Value);

        // Fetch dynamic pricing information from Stripe
        var pricingInfo = new Dictionary<SubscriptionTier, StripePriceInfo>();
        try
        {
            pricingInfo = await _stripeService.GetPricingInfoAsync();
        }
        catch (Exception ex)
        {
            // Log error but continue with fallback pricing
            // In production, you'd want proper logging here
            TempData["Warning"] = $"Unable to load current pricing from Stripe: {ex.Message}. Showing estimated prices.";
        }

        var viewModel = new SubscriptionViewModel
        {
            Subscription = subscription,
            CurrentTier = tier,
            TrialDaysRemaining = trialDaysRemaining,
            IsTrialExpired = isTrialExpired,
            MaxProperties = _subscriptionService.GetMaxProperties(tier),
            MaxCameras = _subscriptionService.GetMaxCameras(tier),
            MaxPhotos = _subscriptionService.GetMaxPhotos(tier),
            PricingInfo = pricingInfo
        };

        ViewBag.SidebarWide = true;

        return View(viewModel);
    }

    [HttpPost]
    public async Task<IActionResult> Subscribe(SubscriptionTier tier)
    {
        return await ProcessSubscriptionChange(tier, "Subscribe");
    }

    [HttpPost]
    public async Task<IActionResult> Update(SubscriptionTier newTier)
    {
        return await ProcessSubscriptionChange(newTier, "Update");
    }

    private async Task<IActionResult> ProcessSubscriptionChange(SubscriptionTier tier, string action)
    {
        if (_currentUserService.Id is null) return Forbid();

        _logger.LogInformation("Processing {Action} request for user {UserId} to tier {Tier}", action, _currentUserService.Id.Value, tier);

        try
        {
            // Pre-validate subscription change before creating Stripe session
            var currentSubscription = await _subscriptionService.GetUserSubscriptionAsync(_currentUserService.Id.Value);
            var currentTier = await _subscriptionService.GetUserSubscriptionTierAsync(_currentUserService.Id.Value);
            
            _logger.LogInformation("User {UserId} current subscription state: Tier={CurrentTier}, SubscriptionId={SubscriptionId}, Status={Status}", 
                _currentUserService.Id.Value, currentTier, currentSubscription?.Id, currentSubscription?.Status);
            
            // Validate tier change is allowed
            if (!IsValidTierChange(currentTier, tier))
            {
                _logger.LogWarning("Invalid tier change attempted by user {UserId}: {CurrentTier} -> {NewTier}", 
                    _currentUserService.Id.Value, currentTier, tier);
                TempData["Error"] = $"Invalid subscription change: Cannot change from {currentTier} to {tier}";
                return RedirectToAction("Index");
            }

            // Check if pricing is available for this tier
            var priceInfo = await _stripeService.GetPriceInfoAsync(tier);
            if (priceInfo == null || !priceInfo.IsActive)
            {
                _logger.LogWarning("Attempted to subscribe to unavailable tier {Tier} by user {UserId}. PriceInfo: {@PriceInfo}", 
                    tier, _currentUserService.Id.Value, priceInfo);
                TempData["Error"] = $"The {tier} plan is currently unavailable. Please contact support or try a different plan.";
                return RedirectToAction("Index");
            }

            _logger.LogInformation("Price validation successful for tier {Tier}: {PriceId} - ${Amount} {Currency}", 
                tier, priceInfo.PriceId, priceInfo.Amount, priceInfo.Currency);

            var successUrl = Url.Action("Success", "Subscription", null, Request.Scheme);
            var cancelUrl = Url.Action("Index", "Subscription", null, Request.Scheme);

            string checkoutUrl;
            
            // Determine whether to create new subscription or update existing
            if (currentTier == SubscriptionTier.Trial || currentSubscription?.StripeSubscriptionId == null)
            {
                _logger.LogInformation("Creating new subscription for user {UserId} to tier {Tier}", _currentUserService.Id.Value, tier);
                checkoutUrl = await _subscriptionService.CreateSubscriptionAsync(
                    _currentUserService.Id.Value, 
                    tier, 
                    successUrl!, 
                    cancelUrl!);
            }
            else
            {
                _logger.LogInformation("Updating existing subscription for user {UserId} from {CurrentTier} to {NewTier}", 
                    _currentUserService.Id.Value, currentTier, tier);
                checkoutUrl = await _subscriptionService.UpdateSubscriptionAsync(
                    _currentUserService.Id.Value, 
                    tier, 
                    successUrl!, 
                    cancelUrl!);
            }

            _logger.LogInformation("Successfully created checkout URL for user {UserId} tier {Tier}: {CheckoutUrl}", 
                _currentUserService.Id.Value, tier, checkoutUrl);

            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            var errorMessage = action == "Subscribe" 
                ? $"Error creating subscription for {tier} plan: {ex.Message}"
                : $"Error updating subscription to {tier} plan: {ex.Message}";
            
            _logger.LogError(ex, "Failed to process {Action} for user {UserId} to tier {Tier}: {ErrorMessage}", 
                action, _currentUserService.Id.Value, tier, ex.Message);
            
            TempData["Error"] = errorMessage;
            return RedirectToAction("Index");
        }
    }

    private static bool IsValidTierChange(SubscriptionTier currentTier, SubscriptionTier newTier)
    {
        // Allow any change from Trial or Expired
        if (currentTier is SubscriptionTier.Trial or SubscriptionTier.Expired)
            return true;

        // Don't allow changing to Trial or Expired
        if (newTier is SubscriptionTier.Trial or SubscriptionTier.Expired)
            return false;

        // Don't allow changing to same tier
        if (currentTier == newTier)
            return false;

        return true;
    }

    [HttpGet]
    public IActionResult Success()
    {
        TempData["Success"] = "Your subscription has been successfully processed!";
        return RedirectToAction("Index");
    }

    [HttpGet]
    public IActionResult Cancel()
    {
        TempData["Info"] = "Subscription process was cancelled.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    [AllowAnonymous]
    public async Task<IActionResult> Webhook()
    {
        var json = await new StreamReader(HttpContext.Request.Body).ReadToEndAsync();
        
        try
        {
            _logger.LogInformation("Received Stripe webhook with payload length: {PayloadLength}", json.Length);
            
            // Get the Stripe signature from headers
            var stripeSignature = Request.Headers["Stripe-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(stripeSignature))
            {
                _logger.LogError("Missing Stripe-Signature header in webhook request");
                return BadRequest("Missing Stripe signature");
            }
            
            // Verify the webhook signature and parse the event
            Event stripeEvent;
            try
            {
                stripeEvent = EventUtility.ConstructEvent(json, stripeSignature, _stripeSettings.WebhookSecret);
                _logger.LogInformation("Successfully verified Stripe webhook signature for event: {EventType} with ID: {EventId}", 
                    stripeEvent.Type, stripeEvent.Id);
            }
            catch (StripeException ex)
            {
                _logger.LogError(ex, "Failed to verify Stripe webhook signature. Signature: {Signature}, PayloadLength: {Length}", 
                    stripeSignature, json.Length);
                return BadRequest("Invalid Stripe signature");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to parse Stripe event from JSON: {Json}", json);
                return BadRequest("Invalid event format");
            }

            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    await HandleCheckoutSessionCompletedAsync(stripeEvent);
                    break;
                    
                case "customer.subscription.created":
                case "customer.subscription.updated":
                    await HandleSubscriptionUpdatedAsync(stripeEvent);
                    break;
                    
                case "customer.subscription.deleted":
                    await HandleSubscriptionDeletedAsync(stripeEvent);
                    break;
                    
                default:
                    _logger.LogInformation("Unhandled Stripe event type: {EventType}", stripeEvent.Type);
                    break;
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing Stripe webhook: {ErrorMessage}", ex.Message);
            return StatusCode(500);
        }
    }

    private async Task HandleCheckoutSessionCompletedAsync(Event stripeEvent)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session?.CustomerId == null)
        {
            _logger.LogError("Failed to parse checkout session from webhook event");
            return;
        }

        _logger.LogInformation("Processing checkout session completed for customer: {CustomerId}", session.CustomerId);

        try
        {
            // Find the user by Stripe customer ID
            var subscription = await _context.Subscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StripeCustomerId == session.CustomerId);

            if (subscription == null)
            {
                _logger.LogError("No subscription found for Stripe customer ID: {CustomerId}", session.CustomerId);
                return;
            }

            // Update subscription with Stripe subscription ID if it's a subscription mode session
            if (session.Mode == "subscription" && !string.IsNullOrEmpty(session.SubscriptionId))
            {
                subscription.StripeSubscriptionId = session.SubscriptionId;
                subscription.Status = "active";
                subscription.CurrentPeriodStart = DateTime.UtcNow;
                subscription.CurrentPeriodEnd = DateTime.UtcNow.AddMonths(1);

                // Determine the tier from the subscription
                var tier = await DetermineSubscriptionTierFromStripe(session.SubscriptionId);
                if (tier.HasValue)
                {
                    subscription.Tier = tier.Value;
                    _logger.LogInformation("Updated subscription tier to {Tier} for user {UserId}", tier.Value, subscription.UserId);
                }
                else
                {
                    _logger.LogWarning("Could not determine subscription tier for Stripe subscription {SubscriptionId}", session.SubscriptionId);
                }

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated subscription {SubscriptionId} for user {UserId} to active status with tier {Tier}", 
                    subscription.Id, subscription.UserId, subscription.Tier);
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling checkout session completed for customer {CustomerId}", session.CustomerId);
        }
    }

    private async Task HandleSubscriptionUpdatedAsync(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription?.Id == null)
        {
            _logger.LogError("Failed to parse subscription from webhook event");
            return;
        }

        _logger.LogInformation("Processing subscription update for Stripe subscription: {SubscriptionId}", stripeSubscription.Id);

        try
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

            if (subscription == null)
            {
                _logger.LogError("No local subscription found for Stripe subscription ID: {SubscriptionId}", stripeSubscription.Id);
                return;
            }

            // Update basic subscription details
            subscription.Status = stripeSubscription.Status ?? "active";

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Successfully updated subscription {SubscriptionId} for user {UserId}", 
                subscription.Id, subscription.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription update for Stripe subscription {SubscriptionId}", stripeSubscription.Id);
        }
    }

    private async Task HandleSubscriptionDeletedAsync(Event stripeEvent)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription?.Id == null)
        {
            _logger.LogError("Failed to parse subscription from webhook event");
            return;
        }

        _logger.LogInformation("Processing subscription deletion for Stripe subscription: {SubscriptionId}", stripeSubscription.Id);

        try
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

            if (subscription == null)
            {
                _logger.LogError("No local subscription found for Stripe subscription ID: {SubscriptionId}", stripeSubscription.Id);
                return;
            }

            subscription.Status = "canceled";
            subscription.CanceledAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            
            _logger.LogInformation("Successfully canceled subscription {SubscriptionId} for user {UserId}", 
                subscription.Id, subscription.UserId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error handling subscription deletion for Stripe subscription {SubscriptionId}", stripeSubscription.Id);
        }
    }

    private async Task<SubscriptionTier?> DetermineSubscriptionTierFromStripe(string stripeSubscriptionId)
    {
        try
        {
            var subscriptionService = new Stripe.SubscriptionService();
            var stripeSubscription = await subscriptionService.GetAsync(stripeSubscriptionId);
            
            if (stripeSubscription?.Items?.Data?.FirstOrDefault()?.Price?.Id == null)
            {
                _logger.LogError("No price ID found in Stripe subscription {SubscriptionId}", stripeSubscriptionId);
                return null;
            }

            var priceId = stripeSubscription.Items.Data.First().Price.Id;
            _logger.LogInformation("Found price ID {PriceId} for Stripe subscription {SubscriptionId}", priceId, stripeSubscriptionId);

            // Map price ID to subscription tier
            var priceIds = _stripeSettings.GetPriceIds();
            foreach (var (tierName, tierPriceId) in priceIds)
            {
                if (tierPriceId == priceId)
                {
                    if (Enum.TryParse<SubscriptionTier>(tierName, true, out var tier))
                    {
                        _logger.LogInformation("Mapped price ID {PriceId} to tier {Tier}", priceId, tier);
                        return tier;
                    }
                }
            }

            _logger.LogWarning("Could not map price ID {PriceId} to any subscription tier", priceId);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining subscription tier from Stripe subscription {SubscriptionId}", stripeSubscriptionId);
            return null;
        }
    }
}