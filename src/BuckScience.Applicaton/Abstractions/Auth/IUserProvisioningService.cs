using System.Security.Claims;

namespace BuckScience.Application.Abstractions.Auth;

// Minimal user info extracted from the identity provider
public record AuthUser(
    string Subject,
    string? Email,
    string? FirstName,
    string? LastName,
    string? DisplayName)
{
    public static AuthUser FromPrincipal(ClaimsPrincipal principal)
    {
        var subject = principal.FindFirst("sub")?.Value
                      ?? principal.FindFirst("oid")?.Value
                      ?? string.Empty;

        var email = principal.FindFirst("emails")?.Value
                    ?? principal.FindFirst("email")?.Value;

        var given = principal.FindFirst("given_name")?.Value;
        var family = principal.FindFirst("family_name")?.Value;
        var name = principal.FindFirst("name")?.Value;

        return new AuthUser(subject, email, given, family, name);
    }
}

public interface IUserProvisioningService
{
    Task EnsureUserAsync(AuthUser user, CancellationToken ct = default);
}