using BuckScience.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace BuckScience.Application.Properties;

public static class DeleteProperty
{
    // Enforce ownership: only delete if (id, userId) matches
    public static async Task<bool> HandleAsync(
        int id,
        int userId,
        IAppDbContext db,
        CancellationToken ct)
    {
        var prop = await db.Properties
            .FirstOrDefaultAsync(p => p.Id == id && p.ApplicationUserId == userId, ct);

        if (prop is null)
            return false; // or throw new KeyNotFoundException("Property not found.");

        db.Properties.Remove(prop);
        await db.SaveChangesAsync(ct);
        return true;
    }
}