using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Infrastructure.Auth;

public class UserProvisioningService : IUserProvisioningService
{
    private readonly IAppDbContext _db;

    public UserProvisioningService(IAppDbContext db) => _db = db;

    public async Task EnsureUserAsync(AuthUser user, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(user.Subject))
            return;

        var existing = await _db.ApplicationUsers
            .FirstOrDefaultAsync(u => u.AzureEntraB2CId == user.Subject, ct);

        var first = user.FirstName ?? string.Empty;
        var last = user.LastName ?? string.Empty;

        var display = !string.IsNullOrWhiteSpace(user.DisplayName)
            ? user.DisplayName!
            : $"{first} {last}".Trim();

        var email = user.Email ?? string.Empty;
        if (string.IsNullOrWhiteSpace(display))
            display = string.IsNullOrWhiteSpace(email) ? "User" : email;

        if (existing is null)
        {
            _db.ApplicationUsers.Add(new ApplicationUser
            {
                AzureEntraB2CId = user.Subject,
                Email = email,
                FirstName = first,
                LastName = last,
                DisplayName = display,
                CreatedDate = DateTime.UtcNow,
                TrialStartDate = DateTime.UtcNow
            });
        }
        else
        {
            existing.Email = email;
            existing.FirstName = first;
            existing.LastName = last;
            existing.DisplayName = display;
        }

        await _db.SaveChangesAsync(ct);
    }
}