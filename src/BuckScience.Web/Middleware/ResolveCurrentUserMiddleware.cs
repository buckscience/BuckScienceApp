using System.Security.Claims;
using BuckScience.Application.Abstractions;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BuckScience.Web.Auth;

// Resolves ApplicationUser.Id once per request (async) after authentication
public sealed class ResolveCurrentUserMiddleware
{
    private readonly RequestDelegate _next;
    private readonly ILogger<ResolveCurrentUserMiddleware> _logger;

    public ResolveCurrentUserMiddleware(RequestDelegate next, ILogger<ResolveCurrentUserMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public async Task InvokeAsync(HttpContext context, IAppDbContext db)
    {
        var user = context.User;
        if (user?.Identity?.IsAuthenticated == true &&
            !context.Items.ContainsKey(CurrentUserConstants.AppUserIdItemKey))
        {
            var externalId =
                user.FindFirst("oid")?.Value ??
                user.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value ??
                user.FindFirst(ClaimTypes.NameIdentifier)?.Value ??
                user.FindFirst("sub")?.Value;

            // Enhanced debugging
            Console.WriteLine($"[ResolveCurrentUserMiddleware] Authenticated user found. ExternalId: {externalId}");
            _logger.LogInformation("ResolveCurrentUserMiddleware: Processing authenticated user with ExternalId: {ExternalId}", externalId);
            
            if (!string.IsNullOrWhiteSpace(externalId))
            {
                var appUserId = await db.ApplicationUsers
                    .AsNoTracking()
                    .Where(u => u.AzureEntraB2CId == externalId)
                    .Select(u => (int?)u.Id)
                    .FirstOrDefaultAsync(context.RequestAborted);

                Console.WriteLine($"[ResolveCurrentUserMiddleware] AppUserId lookup result: {appUserId}");
                _logger.LogInformation("ResolveCurrentUserMiddleware: Database lookup result for ExternalId {ExternalId}: UserId = {AppUserId}", externalId, appUserId);

                if (appUserId.HasValue)
                {
                    context.Items[CurrentUserConstants.AppUserIdItemKey] = appUserId.Value;
                    Console.WriteLine($"[ResolveCurrentUserMiddleware] Set context user ID: {appUserId.Value}");
                    _logger.LogInformation("ResolveCurrentUserMiddleware: Successfully set context user ID: {UserId}", appUserId.Value);
                }
                else
                {
                    // If user not found, check if this is the seeded admin user by email
                    var email = user.FindFirst("emails")?.Value ?? user.FindFirst("email")?.Value;
                    Console.WriteLine($"[ResolveCurrentUserMiddleware] User not found by external ID. Checking email: {email}");
                    
                    if (!string.IsNullOrWhiteSpace(email))
                    {
                        // Try to find user by email (for seeded admin user)
                        var userByEmail = await db.ApplicationUsers
                            .AsNoTracking()
                            .Where(u => u.Email == email)
                            .FirstOrDefaultAsync(context.RequestAborted);
                            
                        if (userByEmail != null)
                        {
                            // Update the existing user's Azure ID to match the authenticated user
                            Console.WriteLine($"[ResolveCurrentUserMiddleware] Found user by email. Updating Azure ID from {userByEmail.AzureEntraB2CId} to {externalId}");
                            
                            var userToUpdate = await db.ApplicationUsers.FindAsync(userByEmail.Id);
                            if (userToUpdate != null)
                            {
                                userToUpdate.AzureEntraB2CId = externalId;
                                await db.SaveChangesAsync(context.RequestAborted);
                                
                                context.Items[CurrentUserConstants.AppUserIdItemKey] = userToUpdate.Id;
                                Console.WriteLine($"[ResolveCurrentUserMiddleware] Updated and set context user ID: {userToUpdate.Id}");
                            }
                        }
                        else
                        {
                            _logger.LogWarning("ResolveCurrentUser: No ApplicationUser found for external id {ExternalId} or email {Email}", externalId, email);
                            Console.WriteLine($"[ResolveCurrentUserMiddleware] WARNING: No ApplicationUser found for external id {externalId} or email {email}");
                        }
                    }
                }
            }
            else
            {
                _logger.LogWarning("ResolveCurrentUser: No external subject claim found on authenticated principal.");
                Console.WriteLine("[ResolveCurrentUserMiddleware] WARNING: No external subject claim found on authenticated principal");
            }
        }
        else
        {
            Console.WriteLine($"[ResolveCurrentUserMiddleware] User not authenticated or ID already resolved. Auth: {user?.Identity?.IsAuthenticated}, HasKey: {context.Items.ContainsKey(CurrentUserConstants.AppUserIdItemKey)}");
        }

        await _next(context);
    }
}

public static class ResolveCurrentUserMiddlewareExtensions
{
    public static IApplicationBuilder UseResolveCurrentUser(this IApplicationBuilder app) =>
        app.UseMiddleware<ResolveCurrentUserMiddleware>();
}