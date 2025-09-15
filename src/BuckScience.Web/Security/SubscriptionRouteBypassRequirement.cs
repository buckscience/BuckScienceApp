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
            
            // If this is a subscription route, succeed the requirement without authentication
            if (path.StartsWith("/subscription", StringComparison.OrdinalIgnoreCase))
            {
                context.Succeed(requirement);
                return Task.CompletedTask;
            }
        }
        
        // For non-subscription routes, require authentication
        if (context.User.Identity?.IsAuthenticated == true)
        {
            context.Succeed(requirement);
        }
        
        return Task.CompletedTask;
    }
}