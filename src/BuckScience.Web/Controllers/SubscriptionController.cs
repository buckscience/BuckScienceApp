using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Domain.Enums;
using BuckScience.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
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

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        ICurrentUserService currentUserService,
        IStripeService stripeService,
        IAppDbContext context,
        ILogger<SubscriptionController> logger)
    {
        _subscriptionService = subscriptionService;
        _currentUserService = currentUserService;
        _stripeService = stripeService;
        _context = context;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (_currentUserService.Id == null)
        {
            var isAuthenticated = _currentUserService.IsAuthenticated;
            var email = _currentUserService.Email;
            var azureId = _currentUserService.AzureEntraB2CId;
            
            _logger.LogError("User ID resolution failed in subscription index. Auth: {IsAuthenticated}, Email: {Email}, AzureId: {AzureId}", 
                isAuthenticated, email, azureId);
            
            TempData["Error"] = $"Unable to load subscription information due to user identification issues. Please try logging out and back in, or contact support. (Auth: {isAuthenticated}, Email: {email})";
            return RedirectToAction("Index", "Home");
        }

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
        if (_currentUserService.Id == null)
        {
            var isAuthenticated = _currentUserService.IsAuthenticated;
            var email = _currentUserService.Email;
            var azureId = _currentUserService.AzureEntraB2CId;
            
            _logger.LogError("User ID resolution failed during subscription upgrade. Auth: {IsAuthenticated}, Email: {Email}, AzureId: {AzureId}", 
                isAuthenticated, email, azureId);
            
            TempData["Error"] = $"Unable to process subscription change due to user identification issues. Please try logging out and back in, or contact support. (Auth: {isAuthenticated}, Email: {email})";
            return RedirectToAction("Index");
        }

        try
        {
            // Pre-validate subscription change before creating Stripe session
            var currentSubscription = await _subscriptionService.GetUserSubscriptionAsync(_currentUserService.Id.Value);
            var currentTier = await _subscriptionService.GetUserSubscriptionTierAsync(_currentUserService.Id.Value);
            
            // Validate tier change is allowed
            if (!IsValidTierChange(currentTier, tier))
            {
                TempData["Error"] = $"Invalid subscription change: Cannot change from {currentTier} to {tier}";
                return RedirectToAction("Index");
            }

            // Check if pricing is available for this tier
            var priceInfo = await _stripeService.GetPriceInfoAsync(tier);
            if (priceInfo == null || !priceInfo.IsActive)
            {
                TempData["Error"] = $"The {tier} plan is currently unavailable. Please contact support or try a different plan.";
                return RedirectToAction("Index");
            }

            var successUrl = Url.Action("Success", "Subscription", null, Request.Scheme);
            var cancelUrl = Url.Action("Index", "Subscription", null, Request.Scheme);

            string checkoutUrl;
            
            // Determine whether to create new subscription or update existing
            if (currentTier == SubscriptionTier.Trial || currentSubscription?.StripeSubscriptionId == null)
            {
                checkoutUrl = await _subscriptionService.CreateSubscriptionAsync(
                    _currentUserService.Id.Value, 
                    tier, 
                    successUrl!, 
                    cancelUrl!);
            }
            else
            {
                checkoutUrl = await _subscriptionService.UpdateSubscriptionAsync(
                    _currentUserService.Id.Value, 
                    tier, 
                    successUrl!, 
                    cancelUrl!);
            }

            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            var errorMessage = action == "Subscribe" 
                ? $"Error creating subscription: {ex.Message}"
                : $"Error updating subscription: {ex.Message}";
            
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
            _logger.LogInformation("Received Stripe webhook");
            
            // Parse the Stripe event
            var stripeEvent = EventUtility.ParseEvent(json);
            _logger.LogInformation("Processing Stripe event: {EventType} with ID: {EventId}", stripeEvent.Type, stripeEvent.Id);

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

                await _context.SaveChangesAsync();
                
                _logger.LogInformation("Updated subscription {SubscriptionId} for user {UserId} to active status", 
                    subscription.Id, subscription.UserId);
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
}