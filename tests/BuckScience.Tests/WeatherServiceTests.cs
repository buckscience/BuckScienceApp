using BuckScience.Shared.Configuration;
using BuckScience.Infrastructure.Services;
using BuckScience.Application.Abstractions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using Xunit;

namespace BuckScience.Tests;

public class WeatherServiceTests
{
    [Fact]
    public void RoundCoordinates_ShouldRoundTo2DecimalPlaces()
    {
        // Arrange
        var mockContext = new Mock<IAppDbContext>();
        var mockHttpClient = new HttpClient();
        var mockApiSettings = Options.Create(new WeatherApiSettings
        {
            BaseUrl = "https://test.api.com",
            APIKey = "test-key"
        });
        var mockWeatherSettings = Options.Create(new WeatherSettings
        {
            LocationRoundingPrecision = 2
        });
        var mockLogger = new Mock<ILogger<WeatherService>>();

        var weatherService = new WeatherService(
            mockContext.Object,
            mockHttpClient,
            mockApiSettings,
            mockWeatherSettings,
            mockLogger.Object);

        // Act
        var result = weatherService.RoundCoordinates(40.123456, -74.987654, 2);

        // Assert
        Assert.Equal(40.12, result.RoundedLatitude);
        Assert.Equal(-74.99, result.RoundedLongitude);
    }

    [Fact]
    public void RoundCoordinates_ShouldRoundTo1DecimalPlace()
    {
        // Arrange
        var mockContext = new Mock<IAppDbContext>();
        var mockHttpClient = new HttpClient();
        var mockApiSettings = Options.Create(new WeatherApiSettings
        {
            BaseUrl = "https://test.api.com",
            APIKey = "test-key"
        });
        var mockWeatherSettings = Options.Create(new WeatherSettings
        {
            LocationRoundingPrecision = 1
        });
        var mockLogger = new Mock<ILogger<WeatherService>>();

        var weatherService = new WeatherService(
            mockContext.Object,
            mockHttpClient,
            mockApiSettings,
            mockWeatherSettings,
            mockLogger.Object);

        // Act
        var result = weatherService.RoundCoordinates(40.123456, -74.987654, 1);

        // Assert
        Assert.Equal(40.1, result.RoundedLatitude);
        Assert.Equal(-75.0, result.RoundedLongitude);
    }
}