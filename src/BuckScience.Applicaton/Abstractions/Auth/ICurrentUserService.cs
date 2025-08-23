namespace BuckScience.Application.Abstractions.Auth;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }
    int? Id { get; }                 // ApplicationUser.Id (DB PK) resolved per-request, no DB here
    string? AzureEntraB2CId { get; } // External subject (AAD B2C oid/sub)
    string? Email { get; }
    string? Name { get; }
}