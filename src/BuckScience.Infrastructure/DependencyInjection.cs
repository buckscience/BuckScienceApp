using BuckScience.Application.Abstractions;
using BuckScience.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using NetTopologySuite;
using NetTopologySuite.Geometries;

namespace BuckScience.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var conn = config.GetConnectionString("DefaultConnection")
                  ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(conn, sql => sql.UseNetTopologySuite()));

        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());
        services.AddSingleton<NtsGeometryServices>(_ => NtsGeometryServices.Instance);
        services.AddSingleton<GeometryFactory>(sp =>
            sp.GetRequiredService<NtsGeometryServices>().CreateGeometryFactory(srid: 4326));

        return services;
    }
}