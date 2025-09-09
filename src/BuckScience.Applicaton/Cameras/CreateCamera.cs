using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Application.Cameras;

public static class CreateCamera
{
    public sealed record Command(
        string LocationName,
        string Brand,
        string? Model,
        double Latitude,
        double Longitude,
        float DirectionDegrees = 0f,
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

        var camera = new Camera("Camera Device", cmd.Brand, cmd.Model, cmd.IsActive, DateTime.UtcNow);
        camera.PlaceInProperty(propertyId);
        camera.PlaceAt(cmd.Latitude, cmd.Longitude, cmd.DirectionDegrees, DateTime.UtcNow, cmd.LocationName);

        db.Cameras.Add(camera);
        await db.SaveChangesAsync(ct);
        return camera.Id;
    }
}