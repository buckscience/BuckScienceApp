using BuckScience.Application.Photos;

namespace BuckScience.Tests;

public class PhotoFiltersTests
{
    [Fact]
    public void PhotoFilters_HasWeatherFilters_ReturnsTrueWhenTemperatureFilterSet()
    {
        // Arrange
        var filters = new PhotoFilters
        {
            TemperatureMin = 20.0
        };

        // Act & Assert
        Assert.True(filters.HasWeatherFilters);
        Assert.True(filters.HasAnyFilters);
    }

    [Fact]
    public void PhotoFilters_HasWeatherFilters_ReturnsTrueWhenConditionsFilterSet()
    {
        // Arrange
        var filters = new PhotoFilters
        {
            Conditions = new List<string> { "Clear", "Cloudy" }
        };

        // Act & Assert
        Assert.True(filters.HasWeatherFilters);
        Assert.True(filters.HasAnyFilters);
    }

    [Fact]
    public void PhotoFilters_HasWeatherFilters_ReturnsFalseWhenNoWeatherFiltersSet()
    {
        // Arrange
        var filters = new PhotoFilters
        {
            CameraIds = new List<int> { 1, 2 }
        };

        // Act & Assert
        Assert.False(filters.HasWeatherFilters);
        Assert.True(filters.HasAnyFilters); // Should still have filters, just not weather ones
    }

    [Fact]
    public void PhotoFilters_HasAnyFilters_ReturnsFalseWhenNoFiltersSet()
    {
        // Arrange
        var filters = new PhotoFilters();

        // Act & Assert
        Assert.False(filters.HasWeatherFilters);
        Assert.False(filters.HasAnyFilters);
    }

    [Fact]
    public void PhotoFilters_HasAnyFilters_ReturnsTrueWhenDateFilterSet()
    {
        // Arrange
        var filters = new PhotoFilters
        {
            DateTakenFrom = DateTime.Now.AddDays(-7)
        };

        // Act & Assert
        Assert.False(filters.HasWeatherFilters);
        Assert.True(filters.HasAnyFilters);
    }

    [Fact]
    public void PhotoFilters_AllWeatherRangeFiltersWork()
    {
        // Arrange
        var filters = new PhotoFilters
        {
            TemperatureMin = 10.0,
            TemperatureMax = 30.0,
            WindSpeedMin = 5.0,
            WindSpeedMax = 25.0,
            HumidityMin = 40.0,
            HumidityMax = 90.0,
            PressureMin = 1000.0,
            PressureMax = 1020.0,
            VisibilityMin = 5.0,
            VisibilityMax = 15.0,
            CloudCoverMin = 0.0,
            CloudCoverMax = 50.0,
            MoonPhaseMin = 0.0,
            MoonPhaseMax = 1.0
        };

        // Act & Assert
        Assert.True(filters.HasWeatherFilters);
        Assert.True(filters.HasAnyFilters);
    }
}