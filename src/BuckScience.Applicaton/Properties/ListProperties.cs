using BuckScience.Application.Abstractions;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace BuckScience.Application.Properties;

public static class ListProperties
{
    public sealed record Result(
        int Id,
        string Name,
        double Latitude,
        double Longitude,
        string TimeZone,
        int DayHour,
        int NightHour
    );

    // In the future, add a userId parameter to filter: e.g., string? userId
    public static async Task<IReadOnlyList<Result>> HandleAsync(
        IAppDbContext db,
        CancellationToken ct)
    {
        return await db.Properties
            .AsNoTracking()
            .OrderBy(p => p.Name)
            .Select(p => new Result(
                p.Id,
                p.Name,
                p.Latitude,
                p.Longitude,
                p.TimeZone,
                p.DayHour,
                p.NightHour))
            .ToListAsync(ct);
    }
}