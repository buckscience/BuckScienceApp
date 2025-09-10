using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Infrastructure.Auth;
using BuckScience.Infrastructure.Persistence;
using BuckScience.Infrastructure.Services;
using BuckScience.Shared.Configuration;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
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

        // Configuration options
        services.Configure<WeatherApiSettings>(config.GetSection(WeatherApiSettings.SectionName));
        services.Configure<WeatherSettings>(config.GetSection(WeatherSettings.SectionName));

        // HTTP client for weather API
        services.AddHttpClient<WeatherService>();

        // Weather service
        services.AddScoped<IWeatherService, WeatherService>();

        // If you have IUserProvisioningService in Infrastructure:
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();

        // NTS GeometryFactory (SRID 4326)
        services.AddSingleton<GeometryFactory>(_ =>
            NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326));

        // Onboarding service
        services.AddScoped<IOnboardingService, Application.Onboarding.OnboardingService>();

        // Season month mapping service
        services.AddScoped<ISeasonMonthMappingService, Application.Services.SeasonMonthMappingService>();

        return services;
    }
}