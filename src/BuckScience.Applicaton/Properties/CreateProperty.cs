using BuckScience.Application.Abstractions;
using BuckScience.Application.FeatureWeights;
using BuckScience.Application.Tags;
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
        int userId,
        CancellationToken ct)
    {
        // NTS uses X=Longitude, Y=Latitude
        var point = geometryFactory.CreatePoint(new Coordinate(cmd.Longitude, cmd.Latitude));

        var prop = new Property(cmd.Name, point, cmd.Boundary, cmd.TimeZone, cmd.DayHour, cmd.NightHour);
        prop.AssignOwner(userId); // use the domain method to set the FK

        db.Properties.Add(prop);
        await db.SaveChangesAsync(ct);

        // Assign default tags to the newly created property
        await AssignDefaultTagsToProperty.HandleAsync(prop.Id, db, ct);

        // Materialize all feature weights for the new property
        await MaterializeFeatureWeights.HandleAsync(prop.Id, db, ct);

        return prop.Id;
    }
}