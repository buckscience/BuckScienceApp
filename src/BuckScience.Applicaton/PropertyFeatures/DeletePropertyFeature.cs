using BuckScience.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.PropertyFeatures;

public static class DeletePropertyFeature
{
    public static async Task<bool> HandleAsync(
        int featureId,
        IAppDbContext db,
        int userId,
        CancellationToken ct)
    {
        // Verify feature exists and user has access to the property
        var feature = await db.PropertyFeatures
            .Include(pf => pf.Property)
            .FirstOrDefaultAsync(pf => pf.Id == featureId && pf.Property.ApplicationUserId == userId, ct);

        if (feature is null)
            return false;

        db.PropertyFeatures.Remove(feature);
        await db.SaveChangesAsync(ct);
        return true;
    }
}