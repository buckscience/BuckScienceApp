using System.Security.Claims;
using BuckScience.Application.Abstractions;
using BuckScience.Domain.Enums;
using BuckScience.Web.Security;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BuckScience.Web.Middleware;

public sealed class SetupFlowMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<SetupFlowMiddleware> _logger;

    public SetupFlowMiddleware(RequestDelegate next, ILogger<SetupFlowMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IOnboardingService onboarding)
    {
        var path = context.Request.Path.Value ?? string.Empty;

        // Must run after UseRouting so endpoint metadata is available.
        if (!(context.User.Identity?.IsAuthenticated ?? false))
        {
            _logger.LogDebug("SetupFlow: Skipping for unauthenticated request {Path}", path);
            await _next(context);
            return;
        }

        if (IsStaticOrHealth(path))
        {
            _logger.LogTrace("SetupFlow: Skipping static/health path {Path}", path);
            await _next(context);
            return;
        }

        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<SkipSetupCheckAttribute>() is not null)
        {
            _logger.LogDebug("SetupFlow: Skipping due to [SkipSetupCheck] on endpoint {Endpoint}", endpoint.DisplayName);
            await _next(context);
            return;
        }

        if (!TryGetUserId(context.User, out var userId))
        {
            _logger.LogWarning("SetupFlow: Could not resolve user id from claims. Claims: {Claims}",
                string.Join(", ", context.User.Claims.Select(c => $"{c.Type}={c.Value}")));
            await _next(context);
            return;
        }

        var (state, primaryPropertyId, firstCameraId) =
            await onboarding.GetStateAsync(userId, context.RequestAborted);

        _logger.LogDebug("SetupFlow: State={State}, PrimaryPropertyId={Primary}, FirstCameraId={Camera}, Path={Path}",
            state, primaryPropertyId, firstCameraId, path);

        switch (state)
        {
            case OnboardingState.NeedsProperty:
                if (!IsOnAddProperty(path) && !IsAuthOrError(path))
                {
                    _logger.LogInformation("SetupFlow: Redirecting to add property");
                    context.Response.Redirect("/properties/add");
                    return;
                }
                break;

            case OnboardingState.NeedsCameraOnPrimaryProperty:
                if (!(primaryPropertyId.HasValue &&
                      (IsOnAddCameraForProperty(path, primaryPropertyId.Value) ||
                       IsWithinProperty(path, primaryPropertyId.Value) ||
                       IsAuthOrError(path))))
                {
                    var url = $"/properties/{primaryPropertyId}/cameras/add";
                    _logger.LogInformation("SetupFlow: Redirecting to add camera {Url}", url);
                    context.Response.Redirect(url);
                    return;
                }
                break;

            case OnboardingState.NeedsPhotoOnPrimaryProperty:
                if (!(primaryPropertyId.HasValue &&
                      (IsWithinProperty(path, primaryPropertyId.Value) || IsAuthOrError(path))))
                {
                    var url = $"/properties/{primaryPropertyId}/cameras/{firstCameraId}/photos/add";
                    _logger.LogInformation("SetupFlow: Redirecting to upload photo {Url}", url);
                    context.Response.Redirect(url);
                    return;
                }
                break;

            case OnboardingState.Complete:
            default:
                // allow
                break;
        }

        await _next(context);
    }

    // Try multiple common claim types. Adjust the parsing to your user id type.
    private static bool TryGetUserId(ClaimsPrincipal user, out int id)
    {
        id = 0;
        string? value =
            user.FindFirstValue("uid") ??
            user.FindFirstValue(ClaimTypes.NameIdentifier) ??
            user.FindFirstValue("sub") ??
            user.FindFirstValue("oid"); // AAD

        return int.TryParse(value, out id);
    }

    private static bool IsOnAddProperty(string path) =>
        path.Equals("/properties/add", StringComparison.OrdinalIgnoreCase) ||
        path.Equals("/properties/create", StringComparison.OrdinalIgnoreCase);

    private static bool IsOnAddCameraForProperty(string path, int propertyId) =>
        path.StartsWith($"/properties/{propertyId}/cameras/add", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith($"/properties/{propertyId}/cameras/create", StringComparison.OrdinalIgnoreCase);

    private static bool IsWithinProperty(string path, int propertyId) =>
        path.Equals($"/properties/{propertyId}", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith($"/properties/{propertyId}/", StringComparison.OrdinalIgnoreCase);

    private static bool IsAuthOrError(string path) =>
        path.StartsWith("/signin-", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/signout-", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/account", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/error", StringComparison.OrdinalIgnoreCase);

    private static bool IsStaticOrHealth(string path) =>
        path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/img/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/health", StringComparison.OrdinalIgnoreCase);
}