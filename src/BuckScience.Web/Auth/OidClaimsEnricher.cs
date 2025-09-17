using System.Security.Claims;
using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Web.Auth;

public static class OidcClaimsEnricher
{
    // Custom claim to carry ApplicationUser.Id
    public const string AppUserIdClaim = "app_user_id";

    // Wire up in Program.cs with Configure<OpenIdConnectOptions>(scheme, OidcClaimsEnricher.Configure)
    public static void Configure(OpenIdConnectOptions options)
    {
        var prior = options.Events.OnTokenValidated;

        options.Events.OnTokenValidated = async context =>
        {
            if (prior is not null)
                await prior(context);

            var principal = context.Principal;
            if (principal is null) return;

            // Prefer oid, fallback to sub for B2C
            var subject = principal.FindFirst("oid")?.Value
                          ?? principal.FindFirst("sub")?.Value;
            if (string.IsNullOrWhiteSpace(subject)) return;

            var sp = context.HttpContext.RequestServices;

            // 1) Ensure user exists/updated
            var provisioner = sp.GetRequiredService<IUserProvisioningService>();
            await provisioner.EnsureUserAsync(AuthUser.FromPrincipal(principal), context.HttpContext.RequestAborted);

            // 2) Lookup internal PK
            var db = sp.GetRequiredService<IAppDbContext>();
            var appUserId = await db.ApplicationUsers.AsNoTracking()
                .Where(u => u.AzureEntraB2CId == subject)
                .Select(u => u.Id)
                .FirstOrDefaultAsync(context.HttpContext.RequestAborted);

            if (appUserId <= 0) return;

            // 3) Stamp claim
            var identity = (ClaimsIdentity)principal.Identity!;
            var existing = identity.FindFirst(AppUserIdClaim);
            if (existing is not null)
                identity.RemoveClaim(existing);

            identity.AddClaim(new Claim(AppUserIdClaim, appUserId.ToString()));
        };
    }
}