using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Infrastructure.Auth;
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

        // Map the interface to the concrete context registered above
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<AppDbContext>());

        // If you have IUserProvisioningService in Infrastructure:
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();

        // NTS GeometryFactory (WGS84 / SRID 4326)
        services.AddSingleton<GeometryFactory>(sp =>
            NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326));

        // If some components request GeometryFactory specifically, map it to the same instance
        services.AddSingleton<GeometryFactory>(sp =>
            (GeometryFactory)sp.GetRequiredService<GeometryFactory>());

        return services;
    }
}