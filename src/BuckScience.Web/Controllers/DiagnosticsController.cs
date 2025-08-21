using BuckScience.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

[Authorize] // same auth as your real actions
public class DiagnosticsController : Controller
{
    private readonly ICurrentUserService _currentUser;

    public DiagnosticsController(ICurrentUserService currentUser)
    {
        _currentUser = currentUser;
    }

    [HttpGet("/diag/content")]
    public IActionResult ContentOnly() => Content("ok");

    [HttpGet("/diag/view")]
    public IActionResult ViewOnly() => View();

    [HttpGet("/diag/userservice")]
    [AllowAnonymous] // Allow anonymous access to test DI
    public IActionResult UserServiceTest()
    {
        try
        {
            var isAuthenticated = _currentUser.IsAuthenticated;
            var azureId = _currentUser.AzureEntraB2CId ?? "null";
            var email = _currentUser.Email ?? "null";
            var name = _currentUser.Name ?? "null";
            var id = _currentUser.Id?.ToString() ?? "null";

            return Content($"UserService DI Working! IsAuth: {isAuthenticated}, AzureId: {azureId}, Email: {email}, Name: {name}, Id: {id}");
        }
        catch (Exception ex)
        {
            return Content($"UserService DI Failed: {ex.Message}");
        }
    }
}