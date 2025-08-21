using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Infrastructure.Auth;
using BuckScience.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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

        return services;
    }
}