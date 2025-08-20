using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using NetTopologySuite.Geometries;

namespace BuckScience.Application.Properties;

public static class CreateProperty
{
    public sealed record Command(
        string Name,
        double Latitude,
        double Longitude,
        string TimeZone,
        int DayHour,
        int NightHour,
        MultiPolygon? Boundary = null);

    public static async Task<int> HandleAsync(
        Command cmd,
        IAppDbContext db,
        GeometryFactory geometryFactory,
        CancellationToken ct)
    {
        // NTS uses X=lon, Y=lat
        var point = geometryFactory.CreatePoint(new Coordinate(cmd.Longitude, cmd.Latitude));
        var prop = new Property(cmd.Name, point, cmd.Boundary, cmd.TimeZone, cmd.DayHour, cmd.NightHour);

        db.Properties.Add(prop);
        await db.SaveChangesAsync(ct);
        return prop.Id;
    }
}