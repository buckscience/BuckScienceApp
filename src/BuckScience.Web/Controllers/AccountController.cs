using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.Abstractions.Services;
using BuckScience.Domain.Enums;

namespace BuckScience.Web.Controllers;

[Authorize]
public class AccountController : Controller
{
    private readonly ICurrentUserService _currentUser;
    private readonly ISubscriptionService _subscriptionService;
    private readonly ILogger<AccountController> _logger;

    public AccountController(
        ICurrentUserService currentUser,
        ISubscriptionService subscriptionService,
        ILogger<AccountController> logger)
    {
        _currentUser = currentUser;
        _subscriptionService = subscriptionService;
        _logger = logger;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        if (!_currentUser.IsAuthenticated || !_currentUser.Id.HasValue)
        {
            return RedirectToAction("Index", "Home");
        }

        // Get user subscription information for the account overview
        var userTier = await _subscriptionService.GetUserSubscriptionTierAsync(_currentUser.Id.Value);
        
        ViewBag.UserEmail = _currentUser.Email;
        ViewBag.UserName = _currentUser.Name;
        ViewBag.SubscriptionTier = userTier;
        ViewBag.SidebarWide = true;

        return View();
    }

    [HttpGet]
    public IActionResult Settings()
    {
        if (!_currentUser.IsAuthenticated)
        {
            return RedirectToAction("Index", "Home");
        }

        ViewBag.SidebarWide = true;
        return View();
    }
}