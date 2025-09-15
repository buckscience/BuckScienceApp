using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.Abstractions.Services;
using BuckScience.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BuckScience.Web.Controllers;

[Authorize]
public class DemoSubscriptionController : Controller
{
    private readonly ISubscriptionService _subscriptionService;
    private readonly ICurrentUserService _currentUserService;

    public DemoSubscriptionController(
        ISubscriptionService subscriptionService,
        ICurrentUserService currentUserService)
    {
        _subscriptionService = subscriptionService;
        _currentUserService = currentUserService;
    }

    [HttpGet]
    public IActionResult Index()
    {
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> SimulateSubscription(SubscriptionTier tier)
    {
        if (_currentUserService.Id == null)
        {
            return Unauthorized();
        }

        try
        {
            // For demo purposes, simulate a successful subscription creation
            // In a real implementation, this would go through Stripe
            TempData["Success"] = $"Demo: Successfully simulated subscription to {tier} plan!";
            return RedirectToAction("Index", "Subscription");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Demo error: {ex.Message}";
            return RedirectToAction("Index", "Subscription");
        }
    }

    [HttpPost]
    public async Task<IActionResult> CheckLimits()
    {
        if (_currentUserService.Id == null)
        {
            return Unauthorized();
        }

        try
        {
            var canAddProperty = await _subscriptionService.CanAddPropertyAsync(_currentUserService.Id.Value);
            var canAddCamera = await _subscriptionService.CanAddCameraAsync(_currentUserService.Id.Value);
            var canUploadPhoto = await _subscriptionService.CanUploadPhotoAsync(_currentUserService.Id.Value);
            var tier = await _subscriptionService.GetUserSubscriptionTierAsync(_currentUserService.Id.Value);
            var trialDays = await _subscriptionService.GetTrialDaysRemainingAsync(_currentUserService.Id.Value);

            var message = $"Demo Limits Check - Tier: {tier}, " +
                         $"Can Add Property: {canAddProperty}, " +
                         $"Can Add Camera: {canAddCamera}, " +
                         $"Can Upload Photo: {canUploadPhoto}, " +
                         $"Trial Days Remaining: {trialDays}";

            TempData["Info"] = message;
            return RedirectToAction("Index", "Subscription");
        }
        catch (Exception ex)
        {
            TempData["Error"] = $"Demo error: {ex.Message}";
            return RedirectToAction("Index", "Subscription");
        }
    }
}