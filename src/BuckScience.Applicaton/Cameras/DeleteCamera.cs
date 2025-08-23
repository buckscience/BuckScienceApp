using BuckScience.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Cameras;

public static class DeleteCamera
{
    public static async Task<bool> HandleAsync(
        int propertyId,
        int id,
        int userId,
        IAppDbContext db,
        CancellationToken ct)
    {
        var camera = await db.Cameras
            .Include(c => c.Property)
            .FirstOrDefaultAsync(c =>
                c.Id == id &&
                c.PropertyId == propertyId &&
                c.Property.ApplicationUserId == userId, ct);

        if (camera is null)
            return false;

        db.Cameras.Remove(camera);
        await db.SaveChangesAsync(ct);
        return true;
    }
}