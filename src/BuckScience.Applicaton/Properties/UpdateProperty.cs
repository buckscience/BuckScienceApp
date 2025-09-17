using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using System.Threading;
using System.Threading.Tasks;

namespace BuckScience.Application.Properties;

public static class UpdateProperty
{
    public sealed record Command(
        int Id,
        string Name,
        double Latitude,
        double Longitude,
        string TimeZone,
        int DayHour,
        int NightHour,
        MultiPolygon? Boundary = null);

    // Enforce ownership here: only update if the property belongs to userId
    public static async Task<bool> HandleAsync(
        Command cmd,
        IAppDbContext db,
        GeometryFactory geometryFactory,
        int userId,
        CancellationToken ct)
    {
        var prop = await db.Properties
            .FirstOrDefaultAsync(p => p.Id == cmd.Id && p.ApplicationUserId == userId, ct);

        if (prop is null)
            return false; // or throw new KeyNotFoundException("Property not found.");

        // Apply updates via domain methods
        prop.Rename(cmd.Name);
        var point = geometryFactory.CreatePoint(new Coordinate(cmd.Longitude, cmd.Latitude));
        prop.SetLocation(point);
        prop.SetBoundary(cmd.Boundary);
        prop.SetHours(cmd.DayHour, cmd.NightHour);

        await db.SaveChangesAsync(ct);
        return true;
    }
}