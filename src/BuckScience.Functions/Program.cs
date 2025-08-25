using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.EntityFrameworkCore;
using BuckScience.Functions.Services;
using Microsoft.Extensions.Logging;

var host = new HostBuilder()
    .ConfigureFunctionsWorkerDefaults()
    .ConfigureServices((context, services) =>
    {
        // Add HTTP client for weather API calls
        services.AddHttpClient();
        
        // Add Entity Framework with SQL Server
        var connectionString = context.Configuration.GetConnectionString("DefaultConnection")
                             ?? throw new InvalidOperationException("DefaultConnection not found.");
        
        services.AddDbContext<WeatherDbContext>(options =>
            options.UseSqlServer(connectionString));
            
        // Add weather cache service
        services.AddScoped<IWeatherCacheService, WeatherCacheService>();
    })
    .Build();

host.Run();
