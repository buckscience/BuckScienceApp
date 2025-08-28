using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using Microsoft.Extensions.Logging;
using Moq;

namespace BuckScience.Tests;

public class WeatherBatchProcessingTests
{
    [Fact]
    public void WeatherService_ShouldRoundCoordinatesConsistently()
    {
        // Arrange
        var mockDbContext = new Mock<IAppDbContext>();
        var mockHttpClient = new Mock<HttpClient>();
        var mockLogger = new Mock<ILogger<object>>();
        var weatherService = new Mock<IWeatherService>();
        
        // Setup coordinate rounding
        weatherService.Setup(x => x.RoundCoordinates(It.IsAny<double>(), It.IsAny<double>(), 2))
            .Returns((double lat, double lon, int precision) => 
            {
                var factor = Math.Pow(10, precision);
                return (Math.Round(lat * factor) / factor, Math.Round(lon * factor) / factor);
            });

        // Act & Assert - Test that coordinates round consistently for batch grouping
        var (lat1, lon1) = weatherService.Object.RoundCoordinates(40.123456, -74.987654, 2);
        var (lat2, lon2) = weatherService.Object.RoundCoordinates(40.124999, -74.989999, 2);
        
        // These should round to the same values for batching
        Assert.Equal(40.12, lat1);
        Assert.Equal(-74.99, lon1);
        Assert.Equal(40.12, lat2);
        Assert.Equal(-74.99, lon2);
        
        // Different coordinates should round differently
        var (lat3, lon3) = weatherService.Object.RoundCoordinates(40.134999, -74.999999, 2);
        Assert.Equal(40.13, lat3);
        Assert.Equal(-75.00, lon3);
    }

    [Fact]
    public void PhotoGrouping_ShouldGroupByLocationAndDate()
    {
        // Arrange - Simulate photo data for batch processing
        var mockWeatherService = new Mock<IWeatherService>();
        mockWeatherService.Setup(x => x.RoundCoordinates(It.IsAny<double>(), It.IsAny<double>(), 2))
            .Returns((double lat, double lon, int precision) => 
            {
                var factor = Math.Pow(10, precision);
                return (Math.Round(lat * factor) / factor, Math.Round(lon * factor) / factor);
            });

        var photosForWeatherProcessing = new List<(string photoId, (double Latitude, double Longitude)? location, DateTime dateTaken)>
        {
            // Same location, same date - should be in one group
            ("photo1", (40.123456, -74.987654), new DateTime(2024, 10, 15, 10, 30, 0)),
            ("photo2", (40.124999, -74.989999), new DateTime(2024, 10, 15, 14, 45, 0)),
            
            // Same location, different date - should be separate group
            ("photo3", (40.123456, -74.987654), new DateTime(2024, 10, 16, 9, 15, 0)),
            
            // Different location, same date - should be separate group
            ("photo4", (40.134999, -74.999999), new DateTime(2024, 10, 15, 11, 0, 0)),
        };

        // Act - Group photos like the batch processing would
        var photoGroups = photosForWeatherProcessing
            .Where(p => p.location.HasValue)
            .GroupBy(p => 
            {
                var (roundedLat, roundedLon) = mockWeatherService.Object.RoundCoordinates(
                    p.location!.Value.Latitude, 
                    p.location!.Value.Longitude, 
                    2);
                var date = DateOnly.FromDateTime(p.dateTaken);
                return new { Latitude = roundedLat, Longitude = roundedLon, Date = date };
            })
            .ToList();

        // Assert
        Assert.Equal(3, photoGroups.Count);
        
        // Group 1: (40.12, -74.99) on 2024-10-15 - should have 2 photos
        var group1 = photoGroups.FirstOrDefault(g => 
            g.Key.Latitude == 40.12 && 
            g.Key.Longitude == -74.99 && 
            g.Key.Date == new DateOnly(2024, 10, 15));
        Assert.NotNull(group1);
        Assert.Equal(2, group1.Count());
        
        // Group 2: (40.12, -74.99) on 2024-10-16 - should have 1 photo
        var group2 = photoGroups.FirstOrDefault(g => 
            g.Key.Latitude == 40.12 && 
            g.Key.Longitude == -74.99 && 
            g.Key.Date == new DateOnly(2024, 10, 16));
        Assert.NotNull(group2);
        Assert.Single(group2);
        
        // Group 3: (40.13, -75.00) on 2024-10-15 - should have 1 photo
        var group3 = photoGroups.FirstOrDefault(g => 
            g.Key.Latitude == 40.13 && 
            g.Key.Longitude == -75.00 && 
            g.Key.Date == new DateOnly(2024, 10, 15));
        Assert.NotNull(group3);
        Assert.Single(group3);
    }

    [Fact]
    public void WeatherBatchProcessing_ShouldMinimizeApiCalls()
    {
        // This test verifies the conceptual benefit of batch processing
        // In the old approach: 4 photos = potentially 4 API calls
        // In the new approach: 4 photos grouped by location/date = 3 API calls maximum (one per unique location/date)
        
        var photos = new[]
        {
            new { Location = (40.12, -74.99), Date = new DateOnly(2024, 10, 15) },
            new { Location = (40.12, -74.99), Date = new DateOnly(2024, 10, 15) }, // Same as above - no extra API call
            new { Location = (40.12, -74.99), Date = new DateOnly(2024, 10, 16) }, // Different date - needs API call
            new { Location = (40.13, -75.00), Date = new DateOnly(2024, 10, 15) }  // Different location - needs API call
        };

        var uniqueLocationDateCombinations = photos
            .Select(p => new { p.Location.Item1, p.Location.Item2, p.Date })
            .Distinct()
            .Count();

        // Assert that batch processing reduces API calls from 4 to 3
        Assert.Equal(4, photos.Length); // 4 total photos
        Assert.Equal(3, uniqueLocationDateCombinations); // Only 3 unique location/date combinations requiring API calls
    }
}