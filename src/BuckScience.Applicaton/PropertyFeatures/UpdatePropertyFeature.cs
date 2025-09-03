using BuckScience.Application.Abstractions;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace BuckScience.Application.PropertyFeatures;

public static class UpdatePropertyFeature
{
    public sealed record Command(
        int Id,
        ClassificationType ClassificationType,
        string GeometryWkt,
        string? Notes = null);

    public static async Task<bool> HandleAsync(
        Command cmd,
        IAppDbContext db,
        GeometryFactory geometryFactory,
        int userId,
        CancellationToken ct)
    {
        // Verify feature exists and user has access to the property
        var feature = await db.PropertyFeatures
            .Include(pf => pf.Property)
            .FirstOrDefaultAsync(pf => pf.Id == cmd.Id && pf.Property.ApplicationUserId == userId, ct);

        if (feature is null)
            return false;

        // Parse geometry from WKT
        var wktReader = new WKTReader();
        Geometry geometry;
        try
        {
            geometry = wktReader.Read(cmd.GeometryWkt);
        }
        catch (Exception ex)
        {
            throw new ArgumentException($"Invalid geometry WKT: {ex.Message}", nameof(cmd.GeometryWkt));
        }

        // Update feature
        feature.UpdateClassificationType(cmd.ClassificationType);
        feature.SetGeometry(geometry);
        feature.UpdateNotes(cmd.Notes);

        await db.SaveChangesAsync(ct);
        return true;
    }
}