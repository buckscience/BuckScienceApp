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

            if (!string.IsNullOrWhiteSpace(externalId))
            {
                var appUserId = await db.ApplicationUsers
                    .AsNoTracking()
                    .Where(u => u.AzureEntraB2CId == externalId)
                    .Select(u => (int?)u.Id)
                    .FirstOrDefaultAsync(context.RequestAborted);

                if (appUserId.HasValue)
                {
                    context.Items[CurrentUserConstants.AppUserIdItemKey] = appUserId.Value;
                }
                else
                {
                    _logger.LogDebug("ResolveCurrentUser: No ApplicationUser found for external id {ExternalId}", externalId);
                }
            }
            else
            {
                _logger.LogDebug("ResolveCurrentUser: No external subject claim found on authenticated principal.");
            }
        }

        await _next(context);
    }
}

public static class ResolveCurrentUserMiddlewareExtensions
{
    public static IApplicationBuilder UseResolveCurrentUser(this IApplicationBuilder app) =>
        app.UseMiddleware<ResolveCurrentUserMiddleware>();
}