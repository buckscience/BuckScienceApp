using BuckScience.Web.ViewModels;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace BuckScience.Web.Controllers;

public class HomeController : Controller
{
    // Public landing page with a Sign in button
    [AllowAnonymous]
    public IActionResult Index() => View();

    // Challenge Azure AD B2C
    [AllowAnonymous]
    public IActionResult SignIn(string? returnUrl = null)
    {
        var redirect = string.IsNullOrWhiteSpace(returnUrl)
            ? Url.Action("Index", "Properties")!
            : returnUrl!;
        return Challenge(new AuthenticationProperties { RedirectUri = redirect }, OpenIdConnectDefaults.AuthenticationScheme);
    }

    // Sign out and return to Home
    [Authorize]
    public IActionResult SignOutUser()
    {
        var props = new AuthenticationProperties { RedirectUri = Url.Action(nameof(Index), "Home")! };
        return SignOut(props, CookieAuthenticationDefaults.AuthenticationScheme, OpenIdConnectDefaults.AuthenticationScheme);
    }

    // Target for UseExceptionHandler("/Home/Error")
    [AllowAnonymous]
    public IActionResult Error()
    {
        var vm = new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier };
        return View("~/Views/Shared/Error.cshtml", vm);
    }
}