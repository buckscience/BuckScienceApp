using BuckScience.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Application.Cameras;

public static class UpdateCamera
{
    public sealed record Command(
        int Id,
        string Name,
        string Brand,
        string? Model,
        double Latitude,
        double Longitude,
        bool IsActive
    );

    // Enforce both property scope and ownership
    public static async Task<bool> HandleAsync(
        Command cmd,
        IAppDbContext db,
        GeometryFactory geometryFactory,
        int userId,
        int propertyId,
        CancellationToken ct)
    {
        var camera = await db.Cameras
            .Include(c => c.Property)
            .FirstOrDefaultAsync(c =>
                c.Id == cmd.Id &&
                c.PropertyId == propertyId &&
                c.Property.ApplicationUserId == userId, ct);

        if (camera is null)
            return false;

        camera.Rename(cmd.Name);
        camera.SetBrand(cmd.Brand);
        camera.SetModel(cmd.Model);

        var point = geometryFactory.CreatePoint(new Coordinate(cmd.Longitude, cmd.Latitude));
        camera.SetLocation(point);

        camera.SetActive(cmd.IsActive);

        await db.SaveChangesAsync(ct);
        return true;
    }
}