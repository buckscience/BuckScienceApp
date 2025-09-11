using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Domain.Enums;
using BuckScience.Web.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuckScience.Web.Controllers;

[Authorize]
public class SubscriptionController : Controller
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ICurrentUserService _currentUserService;
    private readonly IStripeService _stripeService;

    public SubscriptionController(
        ISubscriptionService subscriptionService,
        ICurrentUserService currentUserService,
        IStripeService stripeService)
    {
        _subscriptionService = subscriptionService;
        _currentUserService = currentUserService;
        _stripeService = stripeService;
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
        if (_currentUserService.Id == null)
        {
            return Unauthorized();
        }

        try
        {
            var successUrl = Url.Action("Success", "Subscription", null, Request.Scheme);
            var cancelUrl = Url.Action("Index", "Subscription", null, Request.Scheme);

            var checkoutUrl = await _subscriptionService.CreateSubscriptionAsync(
                _currentUserService.Id.Value, 
                tier, 
                successUrl!, 
                cancelUrl!);

            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error creating subscription: {ex.Message}";
            return RedirectToAction("Index");
        }
    }

    [HttpPost]
    public async Task<IActionResult> Update(SubscriptionTier newTier)
    {
        if (_currentUserService.Id == null)
        {
            // Enhanced error for troubleshooting authentication issues
            var isAuthenticated = _currentUserService.IsAuthenticated;
            var email = _currentUserService.Email;
            var azureId = _currentUserService.AzureEntraB2CId;
            
            TempData["Error"] = $"User authentication failed. Please try logging out and back in. (Auth: {isAuthenticated}, Email: {email})";
            return Unauthorized();
        }

        try
        {
            var successUrl = Url.Action("Success", "Subscription", null, Request.Scheme);
            var cancelUrl = Url.Action("Index", "Subscription", null, Request.Scheme);

            var checkoutUrl = await _subscriptionService.UpdateSubscriptionAsync(
                _currentUserService.Id.Value, 
                newTier, 
                successUrl!, 
                cancelUrl!);

            return Redirect(checkoutUrl);
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Error updating subscription: {ex.Message}";
            return RedirectToAction("Index");
        }
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
        // TODO: Implement Stripe webhook handling
        // This would handle subscription updates, cancellations, etc.
        // For now, return OK to acknowledge receipt
        return Ok();
    }
}