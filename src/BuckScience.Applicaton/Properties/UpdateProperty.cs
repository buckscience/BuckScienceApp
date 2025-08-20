using BuckScience.Application.Abstractions;
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

    public static async Task HandleAsync(
        Command cmd,
        IAppDbContext db,
        GeometryFactory geometryFactory,
        CancellationToken ct)
    {
        var entity = await db.Properties.FirstOrDefaultAsync(p => p.Id == cmd.Id, ct);
        if (entity is null)
            throw new KeyNotFoundException($"Property {cmd.Id} was not found.");

        // Apply domain behaviors
        entity.Rename(cmd.Name);
        entity.Move(cmd.Latitude, cmd.Longitude);
        entity.SetBoundary(cmd.Boundary);
        entity.SetHours(cmd.DayHour, cmd.NightHour);

        // If you need to update time zone in domain, add a method; for now direct set:
        // Consider encapsulating this in a SetTimeZone method if rules emerge
        typeof(BuckScience.Domain.Entities.Property)
            .GetProperty(nameof(BuckScience.Domain.Entities.Property.TimeZone))!
            .SetValue(entity, cmd.TimeZone);

        await db.SaveChangesAsync(ct);
    }
}