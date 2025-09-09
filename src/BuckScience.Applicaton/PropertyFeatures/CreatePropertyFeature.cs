using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;

namespace BuckScience.Application.PropertyFeatures;

public static class CreatePropertyFeature
{
    public sealed record Command(
        int PropertyId,
        ClassificationType ClassificationType,
        string GeometryWkt,
        string? Name = null,
        string? Notes = null,
        float? Weight = null);

    public static async Task<int> HandleAsync(
        Command cmd,
        IAppDbContext db,
        GeometryFactory geometryFactory,
        int userId,
        CancellationToken ct)
    {
        // Verify property ownership
        var property = await db.Properties
            .FirstOrDefaultAsync(p => p.Id == cmd.PropertyId && p.ApplicationUserId == userId, ct);

        if (property is null)
            throw new UnauthorizedAccessException("Property not found or access denied.");

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

        var feature = new PropertyFeature(
            cmd.PropertyId,
            cmd.ClassificationType,
            geometry,
            cmd.Name,
            cmd.Notes,
            cmd.Weight,
            userId);

        db.PropertyFeatures.Add(feature);
        await db.SaveChangesAsync(ct);

        return feature.Id;
    }
}