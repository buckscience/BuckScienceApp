using Azure.Storage.Blobs;
using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Infrastructure.Auth;
using BuckScience.Infrastructure.Options;
using BuckScience.Infrastructure.Persistence;
using BuckScience.Infrastructure.Queues;
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

        // If you have IUserProvisioningService in Infrastructure:
        services.AddScoped<IUserProvisioningService, UserProvisioningService>();

        // NTS GeometryFactory (SRID 4326)
        services.AddSingleton<GeometryFactory>(_ =>
            NtsGeometryServices.Instance.CreateGeometryFactory(srid: 4326));

        // Onboarding service
        services.AddScoped<IOnboardingService, Application.Onboarding.OnboardingService>();

        // Configure Azure Storage options
        var queueSection = config.GetSection(QueueOptions.SectionName);
        services.Configure<QueueOptions>(options =>
        {
            options.ConnectionString = queueSection["ConnectionString"] ?? throw new InvalidOperationException("Queue:ConnectionString not found.");
            options.Name = queueSection["Name"] ?? "photo-ingest";
        });

        // Register Azure Storage services
        var storageConnectionString = config.GetConnectionString("Storage") 
                                     ?? config["Storage:ConnectionString"]
                                     ?? throw new InvalidOperationException("Storage connection string not found.");
        
        services.AddSingleton<BlobServiceClient>(_ => new BlobServiceClient(storageConnectionString));
        services.AddScoped<IPhotoQueue, PhotoQueue>();

        return services;
    }
}