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
        // Use explicit join to avoid LINQ translation errors with navigation properties
        var camera = await db.Cameras
            .Join(db.Properties, c => c.PropertyId, p => p.Id, (c, p) => new { Camera = c, Property = p })
            .Where(x => x.Camera.Id == id && 
                       x.Camera.PropertyId == propertyId && 
                       x.Property.ApplicationUserId == userId)
            .Select(x => x.Camera)
            .FirstOrDefaultAsync(ct);

        if (camera is null)
            return false;

        db.Cameras.Remove(camera);
        await db.SaveChangesAsync(ct);
        return true;
    }
}