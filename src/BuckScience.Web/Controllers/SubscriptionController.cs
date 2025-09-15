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
            _logger.LogInformation("Fetching pricing information from Stripe for subscription index page for user {UserId}", _currentUserService.Id.Value);
            pricingInfo = await _stripeService.GetPricingInfoAsync();
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("Stripe configuration"))
        {
            // Configuration error - show user-friendly message
            _logger.LogError(ex, "Stripe configuration error when loading pricing for user {UserId}: {ErrorMessage}", _currentUserService.Id.Value, ex.Message);
            TempData["Error"] = "Subscription service is currently unavailable due to configuration issues. Please contact support.";
        }
        catch (StripeException ex)
        {
            // Stripe API error - log details but show generic message to user
            _logger.LogError(ex, "Stripe API error when loading pricing for user {UserId}. StripeError: {StripeErrorType} - {StripeErrorCode} - {StripeErrorMessage}", 
                _currentUserService.Id.Value, ex.StripeError?.Type, ex.StripeError?.Code, ex.StripeError?.Message);
            TempData["Warning"] = "Unable to load current pricing from Stripe. Showing estimated prices. Please refresh the page or contact support if the issue persists.";
        }
        catch (Exception ex)
        {
            // Unexpected error - log details but don't expose internals to user
            _logger.LogError(ex, "Unexpected error loading pricing for user {UserId}: {ErrorMessage}", _currentUserService.Id.Value, ex.Message);
            TempData["Warning"] = "Unable to load current pricing information. Please refresh the page or contact support if the issue persists.";
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
        catch (InvalidOperationException ex) when (ex.Message.Contains("Stripe configuration") || ex.Message.Contains("No valid price ID"))
        {
            // Configuration or pricing errors - user-friendly message
            var errorMessage = action == "Subscribe" 
                ? $"Unable to process subscription for {tier} plan due to configuration issues. Please contact support."
                : $"Unable to update subscription to {tier} plan due to configuration issues. Please contact support.";
            
            _logger.LogError(ex, "Configuration error during {Action} for user {UserId} to tier {Tier}: {ErrorMessage}", 
                action, _currentUserService.Id.Value, tier, ex.Message);
            
            TempData["Error"] = errorMessage;
            return RedirectToAction("Index");
        }
        catch (Stripe.StripeException ex)
        {
            // Stripe API errors - log details but show generic message
            var errorMessage = action == "Subscribe" 
                ? $"Unable to process subscription for {tier} plan. Please try again or contact support."
                : $"Unable to update subscription to {tier} plan. Please try again or contact support.";
            
            _logger.LogError(ex, "Stripe API error during {Action} for user {UserId} to tier {Tier}. StripeError: {StripeErrorType} - {StripeErrorCode} - {StripeErrorMessage}", 
                action, _currentUserService.Id.Value, tier, ex.StripeError?.Type, ex.StripeError?.Code, ex.StripeError?.Message);
            
            TempData["Error"] = errorMessage;
            return RedirectToAction("Index");
        }
        catch (Exception ex)
        {
            // Unexpected errors - log details but don't expose internals
            var errorMessage = action == "Subscribe" 
                ? $"An unexpected error occurred while processing your {tier} subscription. Please try again or contact support."
                : $"An unexpected error occurred while updating your subscription to {tier}. Please try again or contact support.";
            
            _logger.LogError(ex, "Unexpected error during {Action} for user {UserId} to tier {Tier}: {ErrorMessage}", 
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
        var requestId = Guid.NewGuid().ToString("N")[..8]; // Short request ID for correlation
        
        _logger.LogInformation("Webhook {RequestId}: Received Stripe webhook with payload length: {PayloadLength}", requestId, json.Length);
        
        try
        {
            // Get the Stripe signature from headers
            var stripeSignature = Request.Headers["Stripe-Signature"].FirstOrDefault();
            if (string.IsNullOrEmpty(stripeSignature))
            {
                _logger.LogError("Webhook {RequestId}: Missing Stripe-Signature header in webhook request", requestId);
                return BadRequest("Missing Stripe signature");
            }
            
            // Verify the webhook signature and parse the event
            Stripe.Event stripeEvent;
            try
            {
                stripeEvent = Stripe.EventUtility.ConstructEvent(json, stripeSignature, _stripeSettings.WebhookSecret);
                _logger.LogInformation("Webhook {RequestId}: Successfully verified Stripe webhook signature for event: {EventType} with ID: {EventId}", 
                    requestId, stripeEvent.Type, stripeEvent.Id);
            }
            catch (Stripe.StripeException ex)
            {
                _logger.LogError(ex, "Webhook {RequestId}: Failed to verify Stripe webhook signature. Signature: {Signature}, PayloadLength: {Length}", 
                    requestId, stripeSignature, json.Length);
                return BadRequest("Invalid Stripe signature");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Webhook {RequestId}: Failed to parse Stripe event from JSON", requestId);
                return BadRequest("Invalid event format");
            }

            // Check for duplicate events (basic deduplication)
            if (await IsEventAlreadyProcessedAsync(stripeEvent.Id))
            {
                _logger.LogInformation("Webhook {RequestId}: Event {EventId} already processed, skipping", requestId, stripeEvent.Id);
                return Ok(); // Return success for duplicate events
            }

            // Process the event based on type
            bool processed = false;
            switch (stripeEvent.Type)
            {
                case "checkout.session.completed":
                    processed = await HandleCheckoutSessionCompletedAsync(stripeEvent, requestId);
                    break;
                    
                case "customer.subscription.created":
                case "customer.subscription.updated":
                    processed = await HandleSubscriptionUpdatedAsync(stripeEvent, requestId);
                    break;
                    
                case "customer.subscription.deleted":
                    processed = await HandleSubscriptionDeletedAsync(stripeEvent, requestId);
                    break;
                    
                default:
                    _logger.LogInformation("Webhook {RequestId}: Unhandled Stripe event type: {EventType}", requestId, stripeEvent.Type);
                    processed = true; // Consider unhandled events as "processed" to avoid retries
                    break;
            }

            if (processed)
            {
                // Mark event as processed for deduplication
                await MarkEventAsProcessedAsync(stripeEvent.Id);
                _logger.LogInformation("Webhook {RequestId}: Successfully processed event {EventId} of type {EventType}", 
                    requestId, stripeEvent.Id, stripeEvent.Type);
            }
            else
            {
                _logger.LogError("Webhook {RequestId}: Failed to process event {EventId} of type {EventType}", 
                    requestId, stripeEvent.Id, stripeEvent.Type);
                return StatusCode(500, "Failed to process webhook event");
            }

            return Ok();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook {RequestId}: Unexpected error processing Stripe webhook: {ErrorMessage}", requestId, ex.Message);
            return StatusCode(500, "Internal server error processing webhook");
        }
    }

    private async Task<bool> HandleCheckoutSessionCompletedAsync(Stripe.Event stripeEvent, string requestId)
    {
        var session = stripeEvent.Data.Object as Session;
        if (session?.CustomerId == null)
        {
            _logger.LogError("Webhook {RequestId}: Failed to parse checkout session from webhook event or missing customer ID", requestId);
            return false;
        }

        _logger.LogInformation("Webhook {RequestId}: Processing checkout session completed for customer: {CustomerId}, session: {SessionId}, mode: {Mode}", 
            requestId, session.CustomerId, session.Id, session.Mode);

        using var transaction = await ((DbContext)_context).Database.BeginTransactionAsync();
        try
        {
            // Find the user by Stripe customer ID
            var subscription = await _context.Subscriptions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.StripeCustomerId == session.CustomerId);

            if (subscription == null)
            {
                _logger.LogError("Webhook {RequestId}: No subscription found for Stripe customer ID: {CustomerId}", requestId, session.CustomerId);
                return false;
            }

            // Update subscription with Stripe subscription ID if it's a subscription mode session
            if (session.Mode == "subscription" && !string.IsNullOrEmpty(session.SubscriptionId))
            {
                // Get detailed subscription information from Stripe to set proper dates
                var stripeSubscriptionService = new Stripe.SubscriptionService();
                var stripeSubscription = await stripeSubscriptionService.GetAsync(session.SubscriptionId);
                
                subscription.StripeSubscriptionId = session.SubscriptionId;
                subscription.Status = stripeSubscription.Status ?? "active";
                
                // Use actual subscription dates from Stripe instead of hardcoded values
                if (stripeSubscription.CurrentPeriodStart.HasValue)
                    subscription.CurrentPeriodStart = stripeSubscription.CurrentPeriodStart.Value;
                if (stripeSubscription.CurrentPeriodEnd.HasValue)
                    subscription.CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd.Value;

                // Determine the tier from the subscription with enhanced logic
                var tier = await DetermineSubscriptionTierFromStripeAsync(session.SubscriptionId, requestId);
                if (tier.HasValue)
                {
                    subscription.Tier = tier.Value;
                    _logger.LogInformation("Webhook {RequestId}: Updated subscription tier to {Tier} for user {UserId}", requestId, tier.Value, subscription.UserId);
                }
                else
                {
                    _logger.LogWarning("Webhook {RequestId}: Could not determine subscription tier for Stripe subscription {SubscriptionId}, keeping existing tier {Tier}", 
                        requestId, session.SubscriptionId, subscription.Tier);
                }

                // Save changes within transaction
                var changesCount = await _context.SaveChangesAsync();
                await transaction.CommitAsync();
                
                _logger.LogInformation("Webhook {RequestId}: Updated subscription {SubscriptionId} for user {UserId} to status {Status} with tier {Tier}. Period: {PeriodStart} to {PeriodEnd}. Database changes: {ChangesCount}", 
                    requestId, subscription.Id, subscription.UserId, subscription.Status, subscription.Tier, 
                    subscription.CurrentPeriodStart, subscription.CurrentPeriodEnd, changesCount);
                
                return true;
            }
            else
            {
                _logger.LogWarning("Webhook {RequestId}: Checkout session {SessionId} is not a subscription mode or missing subscription ID. Mode: {Mode}, SubscriptionId: {SubscriptionId}", 
                    requestId, session.Id, session.Mode, session.SubscriptionId);
                await transaction.CommitAsync();
                return true; // Not an error, just not a subscription checkout
            }
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Webhook {RequestId}: Error handling checkout session completed for customer {CustomerId}, session {SessionId}: {ErrorMessage}", 
                requestId, session.CustomerId, session.Id, ex.Message);
            return false;
        }
    }

    private async Task<bool> HandleSubscriptionUpdatedAsync(Stripe.Event stripeEvent, string requestId)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription?.Id == null)
        {
            _logger.LogError("Webhook {RequestId}: Failed to parse subscription from webhook event or missing subscription ID", requestId);
            return false;
        }

        _logger.LogInformation("Webhook {RequestId}: Processing subscription update for Stripe subscription: {SubscriptionId}, status: {Status}, customer: {CustomerId}", 
            requestId, stripeSubscription.Id, stripeSubscription.Status, stripeSubscription.CustomerId);

        using var transaction = await ((DbContext)_context).Database.BeginTransactionAsync();
        try
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

            if (subscription == null)
            {
                _logger.LogError("Webhook {RequestId}: No local subscription found for Stripe subscription ID: {SubscriptionId}", requestId, stripeSubscription.Id);
                return false;
            }

            // Update subscription details from Stripe data
            var previousStatus = subscription.Status;
            var previousTier = subscription.Tier;
            
            subscription.Status = stripeSubscription.Status ?? "active";
            
            // Update period dates if available
            if (stripeSubscription.CurrentPeriodStart.HasValue)
                subscription.CurrentPeriodStart = stripeSubscription.CurrentPeriodStart.Value;
            if (stripeSubscription.CurrentPeriodEnd.HasValue)
                subscription.CurrentPeriodEnd = stripeSubscription.CurrentPeriodEnd.Value;

            // Update subscription tier based on current price
            var updatedTier = await DetermineSubscriptionTierFromStripeAsync(stripeSubscription.Id, requestId);
            if (updatedTier.HasValue)
            {
                subscription.Tier = updatedTier.Value;
            }

            // Set cancellation date if subscription was canceled
            if (stripeSubscription.Status == "canceled" && stripeSubscription.CanceledAt.HasValue)
            {
                subscription.CanceledAt = stripeSubscription.CanceledAt.Value;
            }

            var changesCount = await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger.LogInformation("Webhook {RequestId}: Successfully updated subscription {SubscriptionId} for user {UserId}. Status: {PreviousStatus} -> {NewStatus}, Tier: {PreviousTier} -> {NewTier}. Database changes: {ChangesCount}", 
                requestId, subscription.Id, subscription.UserId, previousStatus, subscription.Status, previousTier, subscription.Tier, changesCount);
            
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Webhook {RequestId}: Error handling subscription update for Stripe subscription {SubscriptionId}: {ErrorMessage}", 
                requestId, stripeSubscription.Id, ex.Message);
            return false;
        }
    }

    private async Task<bool> HandleSubscriptionDeletedAsync(Stripe.Event stripeEvent, string requestId)
    {
        var stripeSubscription = stripeEvent.Data.Object as Stripe.Subscription;
        if (stripeSubscription?.Id == null)
        {
            _logger.LogError("Webhook {RequestId}: Failed to parse subscription from webhook event or missing subscription ID", requestId);
            return false;
        }

        _logger.LogInformation("Webhook {RequestId}: Processing subscription deletion for Stripe subscription: {SubscriptionId}, canceled at: {CanceledAt}", 
            requestId, stripeSubscription.Id, stripeSubscription.CanceledAt);

        using var transaction = await ((DbContext)_context).Database.BeginTransactionAsync();
        try
        {
            var subscription = await _context.Subscriptions
                .FirstOrDefaultAsync(s => s.StripeSubscriptionId == stripeSubscription.Id);

            if (subscription == null)
            {
                _logger.LogError("Webhook {RequestId}: No local subscription found for Stripe subscription ID: {SubscriptionId}", requestId, stripeSubscription.Id);
                return false;
            }

            var previousStatus = subscription.Status;
            subscription.Status = "canceled";
            
            // Set cancellation date from Stripe data
            if (stripeSubscription.CanceledAt.HasValue)
                subscription.CanceledAt = stripeSubscription.CanceledAt.Value;
            else
                subscription.CanceledAt = DateTime.UtcNow;

            var changesCount = await _context.SaveChangesAsync();
            await transaction.CommitAsync();
            
            _logger.LogInformation("Webhook {RequestId}: Successfully canceled subscription {SubscriptionId} for user {UserId}. Status: {PreviousStatus} -> canceled. Canceled at: {CanceledAt}. Database changes: {ChangesCount}", 
                requestId, subscription.Id, subscription.UserId, previousStatus, subscription.CanceledAt, changesCount);
            
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Webhook {RequestId}: Error handling subscription deletion for Stripe subscription {SubscriptionId}: {ErrorMessage}", 
                requestId, stripeSubscription.Id, ex.Message);
            return false;
        }
    }

    private async Task<SubscriptionTier?> DetermineSubscriptionTierFromStripeAsync(string stripeSubscriptionId, string requestId)
    {
        try
        {
            var subscriptionService = new Stripe.SubscriptionService();
            var stripeSubscription = await subscriptionService.GetAsync(stripeSubscriptionId);
            
            if (stripeSubscription?.Items?.Data?.FirstOrDefault()?.Price?.Id == null)
            {
                _logger.LogError("Webhook {RequestId}: No price ID found in Stripe subscription {SubscriptionId}", requestId, stripeSubscriptionId);
                return null;
            }

            var priceId = stripeSubscription.Items.Data.First().Price.Id;
            _logger.LogInformation("Webhook {RequestId}: Found price ID {PriceId} for Stripe subscription {SubscriptionId}", requestId, priceId, stripeSubscriptionId);

            // Map price ID to subscription tier with enhanced matching
            var priceIds = _stripeSettings.GetPriceIds();
            foreach (var (tierName, tierPriceId) in priceIds)
            {
                if (string.Equals(tierPriceId, priceId, StringComparison.OrdinalIgnoreCase))
                {
                    if (Enum.TryParse<SubscriptionTier>(tierName, true, out var tier))
                    {
                        _logger.LogInformation("Webhook {RequestId}: Mapped price ID {PriceId} to tier {Tier}", requestId, priceId, tier);
                        return tier;
                    }
                    else
                    {
                        _logger.LogError("Webhook {RequestId}: Failed to parse tier name '{TierName}' to SubscriptionTier enum for price {PriceId}", 
                            requestId, tierName, priceId);
                    }
                }
            }

            _logger.LogWarning("Webhook {RequestId}: Could not map price ID {PriceId} to any configured subscription tier. Available tiers: {AvailableTiers}", 
                requestId, priceId, string.Join(", ", priceIds.Select(p => $"{p.Key}={p.Value}")));
            return null;
        }
        catch (Stripe.StripeException ex)
        {
            _logger.LogError(ex, "Webhook {RequestId}: Stripe error determining subscription tier from subscription {SubscriptionId}. StripeError: {StripeErrorType} - {StripeErrorCode} - {StripeErrorMessage}", 
                requestId, stripeSubscriptionId, ex.StripeError?.Type, ex.StripeError?.Code, ex.StripeError?.Message);
            return null;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Webhook {RequestId}: Unexpected error determining subscription tier from Stripe subscription {SubscriptionId}", 
                requestId, stripeSubscriptionId);
            return null;
        }
    }

    /// <summary>
    /// Simple event deduplication to prevent processing the same webhook multiple times.
    /// In a production system, this should be more sophisticated (e.g., using Redis or database).
    /// </summary>
    private static readonly HashSet<string> ProcessedEvents = new();
    private static readonly object ProcessedEventsLock = new();

    private Task<bool> IsEventAlreadyProcessedAsync(string eventId)
    {
        lock (ProcessedEventsLock)
        {
            return Task.FromResult(ProcessedEvents.Contains(eventId));
        }
    }

    private Task MarkEventAsProcessedAsync(string eventId)
    {
        lock (ProcessedEventsLock)
        {
            ProcessedEvents.Add(eventId);
            
            // Simple cleanup: remove old events if we have too many
            if (ProcessedEvents.Count > 1000)
            {
                var oldEvents = ProcessedEvents.Take(500).ToList();
                foreach (var oldEvent in oldEvents)
                {
                    ProcessedEvents.Remove(oldEvent);
                }
            }
        }
        return Task.CompletedTask;
    }
}