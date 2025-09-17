using System.Security.Claims;
using BuckScience.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Http;

namespace BuckScience.Web.Auth;

public sealed class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;
    public CurrentUserService(IHttpContextAccessor http) => _http = http;

    private HttpContext? Http => _http.HttpContext;
    private ClaimsPrincipal? User => Http?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    // Prefer AAD object id; support common OIDC claims
    public string? AzureEntraB2CId =>
        User?.FindFirst("oid")?.Value
        ?? User?.FindFirst("http://schemas.microsoft.com/identity/claims/objectidentifier")?.Value
        ?? User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
        ?? User?.FindFirst("sub")?.Value;

    public string? Email =>
        User?.FindFirst("emails")?.Value
        ?? User?.FindFirst("email")?.Value;

    public string? Name
    {
        get
        {
            var name = User?.FindFirst("name")?.Value;
            if (!string.IsNullOrWhiteSpace(name)) return name;

            var given = User?.FindFirst("given_name")?.Value;
            var family = User?.FindFirst("family_name")?.Value;
            var combined = $"{given} {family}".Trim();
            return string.IsNullOrWhiteSpace(combined) ? null : combined;
        }
    }

    // No DB here. This is set once per request by ResolveCurrentUserMiddleware.
    public int? Id
    {
        get
        {
            if (Http?.Items.TryGetValue(CurrentUserConstants.AppUserIdItemKey, out var boxed) == true)
            {
                // We only store an int value in HttpContext.Items
                if (boxed is int id) return id;

                // If something else got stored (e.g., string), try to parse defensively
                if (boxed is string s && int.TryParse(s, out var parsed)) return parsed;
            }
            return null;
        }
    }
}