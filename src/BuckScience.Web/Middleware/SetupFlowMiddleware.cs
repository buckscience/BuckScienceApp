using System;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Application.Abstractions.Services;
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

    public async Task InvokeAsync(HttpContext context, IOnboardingService onboarding, ICurrentUserService currentUser)
    {
        var path = context.Request.Path.Value ?? string.Empty;
        var method = context.Request.Method;

        _logger.LogDebug("SetupFlow: {Method} {Path}", method, path);

        // Unauthenticated users are not part of onboarding
        if (!currentUser.IsAuthenticated)
        {
            await _next(context);
            return;
        }

        // Allow static content, health checks, auth, error endpoints, and subscription management
        if (IsStaticOrHealth(path) || IsAuthOrError(path) || IsSubscriptionPath(path))
        {
            await _next(context);
            return;
        }

        // Explicit bypass via attribute (works when running after routing)
        var endpoint = context.GetEndpoint();
        if (endpoint?.Metadata?.GetMetadata<SkipSetupCheckAttribute>() is not null)
        {
            await _next(context);
            return;
        }

        // Ensure we can map the principal to an application user
        var userId = currentUser.Id;
        if (!userId.HasValue)
        {
            // For subscription paths, allow the controller to handle user provisioning
            if (IsSubscriptionPath(path))
            {
                _logger.LogInformation("SetupFlow: Allowing subscription path {Path} for user provisioning", path);
                await _next(context);
                return;
            }
            
            _logger.LogWarning("SetupFlow: Authenticated principal has no ApplicationUser.Id mapping. Blocking. Path={Path}", path);
            context.Response.StatusCode = StatusCodes.Status403Forbidden;
            await context.Response.WriteAsync("User is authenticated but not provisioned.");
            return;
        }

        // Compute onboarding state
        var (state, primaryPropertyId, firstCameraId) =
            await onboarding.GetStateAsync(userId.Value, context.RequestAborted);

        // Gather IDs defensively (from route values, query, or path)
        var routePid = TryGetPropertyId(context, path);
        var routeCamId = TryGetCameraId(context, path);

        _logger.LogInformation(
            "SetupFlow: State={State}, PrimaryPid={PrimaryPid}, RoutePid={RoutePid}, FirstCamId={FirstCamId}, EndpointSet={EndpointSet}, {Method} {Path}",
            state, primaryPropertyId, routePid, firstCameraId, endpoint != null, method, path);

        // Allow everything once complete
        if (state == OnboardingState.Complete)
        {
            await _next(context);
            return;
        }

        // NeedsProperty: only allow /properties/add (GET or POST). Redirect all else.
        if (state == OnboardingState.NeedsProperty)
        {
            if (IsPathPropertiesAdd(path))
            {
                await _next(context);
                return;
            }

            var to = "/properties/add?fromSetup=1";
            LogRedirect("NeedsProperty: force step", method, path, to);
            context.Response.Redirect(to);
            return;
        }

        // NeedsCameraOnPrimaryProperty: only allow /properties/{primaryPid}/cameras/add
        if (state == OnboardingState.NeedsCameraOnPrimaryProperty)
        {
            if (!primaryPropertyId.HasValue)
            {
                var to = "/properties/add?fromSetup=1";
                LogRedirect("NeedsCameraOnPrimaryProperty: missing primaryPid", method, path, to);
                context.Response.Redirect(to);
                return;
            }

            var primaryPid = primaryPropertyId.Value;

            if (IsPathCamerasAdd(path))
            {
                if (routePid == primaryPid)
                {
                    await _next(context);
                    return;
                }

                // Normalize to primary property
                var toWrong = $"/properties/{primaryPid}/cameras/add?fromSetup=1";
                LogRedirect("NeedsCameraOnPrimaryProperty: normalize to primary", method, path, toWrong);
                context.Response.Redirect(toWrong);
                return;
            }

            var toForce = $"/properties/{primaryPid}/cameras/add?fromSetup=1";
            LogRedirect("NeedsCameraOnPrimaryProperty: force step", method, path, toForce);
            context.Response.Redirect(toForce);
            return;
        }

        // NeedsPhotoOnPrimaryProperty: allow either property-scoped photos/add OR cameras/{cid}/upload (for the first camera)
        if (state == OnboardingState.NeedsPhotoOnPrimaryProperty)
        {
            if (!primaryPropertyId.HasValue || !firstCameraId.HasValue)
            {
                // Safety fallback – go back a step or to property add
                if (primaryPropertyId.HasValue)
                {
                    var toCam = $"/properties/{primaryPropertyId.Value}/cameras/add?fromSetup=1";
                    LogRedirect("NeedsPhotoOnPrimaryProperty: missing cameraId", method, path, toCam);
                    context.Response.Redirect(toCam);
                }
                else
                {
                    var toProp = "/properties/add?fromSetup=1";
                    LogRedirect("NeedsPhotoOnPrimaryProperty: missing primaryPid", method, path, toProp);
                    context.Response.Redirect(toProp);
                }
                return;
            }

            var primaryPid = primaryPropertyId.Value;
            var camId = firstCameraId.Value;

            // Allow the new CamerasController upload route for the first camera
            if (IsPathPhotoUploadFor(path, camId))
            {
                await _next(context);
                return;
            }

            // Otherwise, drive them to the upload route for the first camera
            var toPhoto = $"/cameras/{camId}/upload?fromSetup=1";
            LogRedirect("NeedsPhotoOnPrimaryProperty: force step", method, path, toPhoto);
            context.Response.Redirect(toPhoto);
            return;
        }

        // Fallback
        await _next(context);
    }

    // ---- Helpers ----

    private static bool IsPathPropertiesAdd(string path) =>
        path.Equals("/properties/add", StringComparison.OrdinalIgnoreCase);

    private static bool IsPathCamerasAdd(string path) =>
        Regex.IsMatch(path, @"^/properties/\d+/cameras/add(/|$)", RegexOptions.IgnoreCase);

    // /cameras/{id}/photos/upload (non property-scoped)
    private static bool IsPathPhotoUploadFor(string path, int requiredCameraId) =>
        Regex.IsMatch(path, $@"^/cameras/{requiredCameraId}/upload(/|$)", RegexOptions.IgnoreCase);

    private static int? TryGetPropertyId(HttpContext context, string path)
    {
        if (context.Request.RouteValues.TryGetValue("propertyId", out var v) &&
            int.TryParse(v?.ToString(), out var parsed))
            return parsed;

        if (context.Request.Query.TryGetValue("propertyId", out var qv) &&
            int.TryParse(qv.ToString(), out var qparsed))
            return qparsed;

        var m = Regex.Match(path, @"^/properties/(\d+)(/|$)", RegexOptions.IgnoreCase);
        if (m.Success && int.TryParse(m.Groups[1].Value, out var id))
            return id;

        return null;
    }

    private static int? TryGetCameraId(HttpContext context, string path)
    {
        if (context.Request.RouteValues.TryGetValue("cameraId", out var v) &&
            int.TryParse(v?.ToString(), out var parsed))
            return parsed;

        if (context.Request.Query.TryGetValue("cameraId", out var qv) &&
            int.TryParse(qv.ToString(), out var qparsed))
            return qparsed;

        // from property-scoped camera routes
        var m1 = Regex.Match(path, @"^/properties/\d+/cameras/(\d+)(/|$)", RegexOptions.IgnoreCase);
        if (m1.Success && int.TryParse(m1.Groups[1].Value, out var id1))
            return id1;

        // from non property-scoped camera routes (e.g., /cameras/{id}/upload)
        var m2 = Regex.Match(path, @"^/cameras/(\d+)(/|$)", RegexOptions.IgnoreCase);
        if (m2.Success && int.TryParse(m2.Groups[1].Value, out var id2))
            return id2;

        return null;
    }

    private static bool IsAuthOrError(string path) =>
        path.StartsWith("/signin-", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/signout-", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/account", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/error", StringComparison.OrdinalIgnoreCase);

    private static bool IsSubscriptionPath(string path) =>
        path.StartsWith("/subscription", StringComparison.OrdinalIgnoreCase);

    private static bool IsStaticOrHealth(string path) =>
        path.StartsWith("/css/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/js/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/img/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/images/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/lib/", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/favicon", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/health", StringComparison.OrdinalIgnoreCase);

    private void LogRedirect(string reason, string fromMethod, string fromPath, string toLocation) =>
        _logger.LogInformation("SetupFlow: Redirecting ({Reason}) {Method} {From} -> {To}",
            reason, fromMethod, fromPath, toLocation);
}