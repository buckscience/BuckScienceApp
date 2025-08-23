using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Application.Cameras;

public static class CreateCamera
{
    public sealed record Command(
        string Name,
        string Brand,
        string? Model,
        double Latitude,
        double Longitude,
        bool IsActive = true
    );

    // propertyId is the route/context value (do not trust payload for it)
    public static async Task<int> HandleAsync(
        Command cmd,
        IAppDbContext db,
        GeometryFactory geometryFactory,
        int userId,
        int propertyId,
        CancellationToken ct)
    {
        // Verify property ownership
        var property = await db.Properties
            .FirstOrDefaultAsync(p => p.Id == propertyId && p.ApplicationUserId == userId, ct);

        if (property is null)
            throw new KeyNotFoundException("Property not found or not owned by user.");

        var point = geometryFactory.CreatePoint(new Coordinate(cmd.Longitude, cmd.Latitude));

        var camera = new Camera(cmd.Name, cmd.Brand, cmd.Model, point, cmd.IsActive, DateTime.UtcNow);
        camera.PlaceInProperty(propertyId);

        db.Cameras.Add(camera);
        await db.SaveChangesAsync(ct);
        return camera.Id;
    }
}