using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Domain.Enums;
using BuckScience.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
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

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        ICurrentUserService currentUserService,
        IStripeService stripeService,
        IAppDbContext context)
    {
        _subscriptionService = subscriptionService;
        _currentUserService = currentUserService;
        _stripeService = stripeService;
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (_currentUserService.Id == null)
        {
            return Unauthorized();
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
            
            TempData["Error"] = $"User authentication failed. Please try logging out and back in. (Auth: {isAuthenticated}, Email: {email})";
            return Unauthorized();
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
            // For now, we'll implement basic webhook handling
            // In production, you should verify the webhook signature
            Console.WriteLine($"Received Stripe webhook: {json}");
            
            // Basic JSON parsing to get event type
            if (json.Contains("checkout.session.completed"))
            {
                Console.WriteLine("Checkout session completed - subscription should be active");
                // TODO: Update subscription status in database
            }
            else if (json.Contains("customer.subscription"))
            {
                Console.WriteLine("Subscription event received");
                // TODO: Handle subscription changes
            }

            return Ok();
        }
        catch (Exception e)
        {
            Console.WriteLine($"Webhook processing error: {e.Message}");
            return StatusCode(500);
        }
    }
}