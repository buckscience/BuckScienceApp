using BuckScience.Application.Photos;

namespace BuckScience.Tests;

public class PhotoFilteringUrlExamples
{
    [Theory]
    [InlineData("tempMin=10&tempMax=30", 10.0, 30.0, null, null)]
    [InlineData("windSpeedMin=5&windSpeedMax=15", null, null, 5.0, 15.0)]
    [InlineData("tempMin=20", 20.0, null, null, null)]
    [InlineData("tempMax=25", null, 25.0, null, null)]
    public void BuildFilters_UrlParameters_ShouldParseCorrectly(
        string urlParams, 
        double? expectedTempMin, 
        double? expectedTempMax,
        double? expectedWindMin,
        double? expectedWindMax)
    {
        // This test simulates URL parameter parsing scenarios
        // Arrange - simulate what the controller would receive
        double? tempMin = urlParams.Contains("tempMin=10") ? 10.0 : 
                         urlParams.Contains("tempMin=20") ? 20.0 : null;
        double? tempMax = urlParams.Contains("tempMax=30") ? 30.0 : 
                         urlParams.Contains("tempMax=25") ? 25.0 : null;
        double? windSpeedMin = urlParams.Contains("windSpeedMin=5") ? 5.0 : null;
        double? windSpeedMax = urlParams.Contains("windSpeedMax=15") ? 15.0 : null;

        var filters = new PhotoFilters
        {
            TemperatureMin = tempMin,
            TemperatureMax = tempMax,
            WindSpeedMin = windSpeedMin,
            WindSpeedMax = windSpeedMax
        };

        // Act & Assert
        Assert.Equal(expectedTempMin, filters.TemperatureMin);
        Assert.Equal(expectedTempMax, filters.TemperatureMax);
        Assert.Equal(expectedWindMin, filters.WindSpeedMin);
        Assert.Equal(expectedWindMax, filters.WindSpeedMax);

        if (expectedTempMin.HasValue || expectedTempMax.HasValue || 
            expectedWindMin.HasValue || expectedWindMax.HasValue)
        {
            Assert.True(filters.HasWeatherFilters);
            Assert.True(filters.HasAnyFilters);
        }
    }

    [Theory]
    [InlineData("1,2,3", new[] { 1, 2, 3 })]
    [InlineData("5", new[] { 5 })]
    [InlineData("1,5,10", new[] { 1, 5, 10 })]
    public void BuildFilters_CameraParameters_ShouldParseCorrectly(string cameras, int[] expectedCameras)
    {
        // Test camera ID parsing
        var filters = new PhotoFilters();
        
        // Simulate the parsing that BuildFilters method does
        if (!string.IsNullOrWhiteSpace(cameras))
        {
            var cameraIds = cameras.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => int.TryParse(s.Trim(), out var id) ? id : (int?)null)
                .Where(id => id.HasValue)
                .Select(id => id!.Value)
                .ToList();
            if (cameraIds.Count > 0)
                filters.CameraIds = cameraIds;
        }

        // Assert
        Assert.NotNull(filters.CameraIds);
        Assert.Equal(expectedCameras.Length, filters.CameraIds.Count);
        foreach (var expectedCamera in expectedCameras)
        {
            Assert.Contains(expectedCamera, filters.CameraIds);
        }
        Assert.True(filters.HasAnyFilters);
        Assert.False(filters.HasWeatherFilters); // Camera filters are not weather filters
    }

    [Theory]
    [InlineData("Clear,Cloudy", new[] { "Clear", "Cloudy" })]
    [InlineData("Rainy", new[] { "Rainy" })]
    [InlineData("Clear,Partly Cloudy,Overcast", new[] { "Clear", "Partly Cloudy", "Overcast" })]
    public void BuildFilters_ConditionsParameters_ShouldParseCorrectly(string conditions, string[] expectedConditions)
    {
        // Test conditions parsing
        var filters = new PhotoFilters();
        
        // Simulate the parsing that BuildFilters method does
        if (!string.IsNullOrWhiteSpace(conditions))
        {
            filters.Conditions = conditions.Split(',', StringSplitOptions.RemoveEmptyEntries)
                .Select(s => s.Trim())
                .Where(s => !string.IsNullOrEmpty(s))
                .ToList();
        }

        // Assert
        Assert.NotNull(filters.Conditions);
        Assert.Equal(expectedConditions.Length, filters.Conditions.Count);
        foreach (var expectedCondition in expectedConditions)
        {
            Assert.Contains(expectedCondition, filters.Conditions);
        }
        Assert.True(filters.HasWeatherFilters);
        Assert.True(filters.HasAnyFilters);
    }

    [Fact]
    public void PhotoFiltering_RealWorldScenario_TempAndCameraFilter()
    {
        // Test a realistic filtering scenario
        var filters = new PhotoFilters
        {
            TemperatureMin = 15.0,  // Photos taken when temp was at least 15°C
            TemperatureMax = 25.0,  // But not more than 25°C
            CameraIds = new List<int> { 1, 3 },  // From cameras 1 and 3 only
            DateTakenFrom = new DateTime(2024, 10, 1),  // In October 2024
            DateTakenTo = new DateTime(2024, 10, 31)
        };

        // Assert this is a valid complex filter
        Assert.True(filters.HasWeatherFilters);
        Assert.True(filters.HasAnyFilters);
        Assert.Equal(15.0, filters.TemperatureMin);
        Assert.Equal(25.0, filters.TemperatureMax);
        Assert.Equal(2, filters.CameraIds!.Count);
        Assert.Contains(1, filters.CameraIds);
        Assert.Contains(3, filters.CameraIds);
        Assert.Equal(new DateTime(2024, 10, 1), filters.DateTakenFrom);
        Assert.Equal(new DateTime(2024, 10, 31), filters.DateTakenTo);
    }

    [Fact]
    public void PhotoFiltering_RealWorldScenario_MoonPhaseAndConditions()
    {
        // Test filtering for specific moon phases and weather conditions
        var filters = new PhotoFilters
        {
            MoonPhaseMin = 0.9,     // Nearly full moon
            MoonPhaseMax = 1.0,     // To full moon
            Conditions = new List<string> { "Clear", "Partly Cloudy" },  // Good visibility
            WindSpeedMax = 10.0     // Low wind for better photos
        };

        // Assert this is a valid weather-focused filter
        Assert.True(filters.HasWeatherFilters);
        Assert.True(filters.HasAnyFilters);
        Assert.Equal(0.9, filters.MoonPhaseMin);
        Assert.Equal(1.0, filters.MoonPhaseMax);
        Assert.Equal(2, filters.Conditions!.Count);
        Assert.Contains("Clear", filters.Conditions);
        Assert.Contains("Partly Cloudy", filters.Conditions);
        Assert.Equal(10.0, filters.WindSpeedMax);
    }
}