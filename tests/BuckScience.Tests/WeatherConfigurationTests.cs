using BuckScience.Shared.Configuration;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Xunit;

namespace BuckScience.Tests;

public class WeatherConfigurationTests
{
    [Fact]
    public void WeatherSettings_ShouldBindFromConfiguration()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            ["WeatherSettings:LocationRoundingPrecision"] = "3",
            ["WeatherSettings:EnableGPSExtraction"] = "false",
            ["WeatherSettings:FallbackToCameraLocation"] = "false"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new ServiceCollection();
        services.Configure<WeatherSettings>(configuration.GetSection(WeatherSettings.SectionName));

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var weatherSettings = serviceProvider.GetRequiredService<IOptions<WeatherSettings>>().Value;

        // Assert
        Assert.Equal(3, weatherSettings.LocationRoundingPrecision);
        Assert.False(weatherSettings.EnableGPSExtraction);
        Assert.False(weatherSettings.FallbackToCameraLocation);
    }

    [Fact]
    public void WeatherApiSettings_ShouldBindFromConfiguration()
    {
        // Arrange
        var configDict = new Dictionary<string, string?>
        {
            ["WeatherAPISettings:BaseUrl"] = "https://test.weather.com/api/",
            ["WeatherAPISettings:APIKey"] = "test-api-key-12345"
        };

        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(configDict)
            .Build();

        var services = new ServiceCollection();
        services.Configure<WeatherApiSettings>(configuration.GetSection(WeatherApiSettings.SectionName));

        var serviceProvider = services.BuildServiceProvider();

        // Act
        var apiSettings = serviceProvider.GetRequiredService<IOptions<WeatherApiSettings>>().Value;

        // Assert
        Assert.Equal("https://test.weather.com/api/", apiSettings.BaseUrl);
        Assert.Equal("test-api-key-12345", apiSettings.APIKey);
    }
}