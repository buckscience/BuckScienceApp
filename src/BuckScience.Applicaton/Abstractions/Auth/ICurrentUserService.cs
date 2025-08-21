namespace BuckScience.Application.Abstractions.Auth;

public interface ICurrentUserService
{
    bool IsAuthenticated { get; }

    // External identity (matches ApplicationUser.AzureEntraB2CId)
    string? AzureEntraB2CId { get; }
    string? Email { get; }
    string? Name { get; }

    // Database identity (ApplicationUser.Id)
    int? Id { get; }
}