using System.Security.Claims;
using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Web.Auth;

public class CurrentUserService : ICurrentUserService
{
    private readonly IHttpContextAccessor _http;
    private readonly IAppDbContext _db;

    // Claim that carries the DB PK (ApplicationUser.Id)
    private const string AppUserIdClaim = "app_user_id";
    // Per-request cache key to avoid repeated lookups
    private const string AppUserIdItemKey = "__app_user_id";

    public CurrentUserService(IHttpContextAccessor http, IAppDbContext db)
    {
        _http = http;
        _db = db;
    }

    private HttpContext? Http => _http.HttpContext;
    private ClaimsPrincipal? User => Http?.User;

    public bool IsAuthenticated => User?.Identity?.IsAuthenticated == true;

    // External identity used to map to ApplicationUser.AzureEntraB2CId
    // Prefer 'oid' (AAD object id), fall back to 'sub' depending on B2C policy
    public string? AzureEntraB2CId =>
        User?.FindFirst("oid")?.Value
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

    // Database PK from ApplicationUser.Id
    public int? Id
    {
        get
        {
            // 1) From claim (best: stamped at sign-in)
            if (int.TryParse(User?.FindFirst(AppUserIdClaim)?.Value, out var idFromClaim))
                return idFromClaim;

            // 2) From per-request cache
            if (Http?.Items.TryGetValue(AppUserIdItemKey, out var boxed) == true && boxed is int cached)
                return cached;

            // 3) Fallback: resolve via AzureEntraB2CId -> DB (once per request)
            var externalId = AzureEntraB2CId;
            if (string.IsNullOrWhiteSpace(externalId)) return null;

            var id = _db.ApplicationUsers.AsNoTracking()
                .Where(u => u.AzureEntraB2CId == externalId)
                .Select(u => (int?)u.Id)
                .FirstOrDefault();

            if (id.HasValue && Http is not null)
                Http.Items[AppUserIdItemKey] = id.Value;

            return id;
        }
    }
}