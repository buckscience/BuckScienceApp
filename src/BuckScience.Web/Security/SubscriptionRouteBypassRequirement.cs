using Microsoft.AspNetCore.Authorization;

namespace BuckScience.Web.Security;

/// <summary>
/// Authorization requirement that allows bypassing authentication for subscription routes.
/// This prevents the fallback authorization policy from triggering auth challenges 
/// for subscription-related requests.
/// </summary>
public class SubscriptionRouteBypassRequirement : IAuthorizationRequirement
{
}

/// <summary>
/// Authorization handler that bypasses authentication requirements for subscription routes
/// to prevent auth challenges from being triggered by the fallback policy.
/// </summary>
public class SubscriptionRouteBypassHandler : AuthorizationHandler<SubscriptionRouteBypassRequirement>
{
    protected override Task HandleRequirementAsync(
        AuthorizationHandlerContext context,
        SubscriptionRouteBypassRequirement requirement)
    {
        // Get the HTTP context
        if (context.Resource is HttpContext httpContext)
        {
            var path = httpContext.Request.Path.Value ?? string.Empty;
            var method = httpContext.Request.Method;
            
            // Enhanced logging for debugging authorization issues
            var logger = httpContext.RequestServices.GetService<ILogger<SubscriptionRouteBypassHandler>>();
            logger?.LogInformation("SubscriptionRouteBypass: Checking {Method} {Path}, User.IsAuthenticated={IsAuth}", 
                method, path, context.User.Identity?.IsAuthenticated);
            
            // If this is a subscription route, succeed the requirement without authentication
            if (path.StartsWith("/subscription", StringComparison.OrdinalIgnoreCase))
            {
                logger?.LogInformation("SubscriptionRouteBypass: ALLOWING subscription path {Path} - bypassing authentication requirement", path);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
            
            // Also check for capital S in case of routing differences
            if (path.StartsWith("/Subscription", StringComparison.OrdinalIgnoreCase))
            {
                logger?.LogInformation("SubscriptionRouteBypass: ALLOWING subscription path {Path} (capital S) - bypassing authentication requirement", path);
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }
        
        // For non-subscription routes, require authentication
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
        }
        else
        {
            // Log when authentication is required for non-subscription routes
            if (context.Resource is HttpContext ctx)
            {
                var logger = ctx.RequestServices.GetService<ILogger<SubscriptionRouteBypassHandler>>();
                logger?.LogWarning("SubscriptionRouteBypass: REQUIRING authentication for {Path} - user not authenticated", 
                    ctx.Request.Path.Value);
            }
        }
        
        return Task.CompletedTask;
    }
}