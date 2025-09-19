using BuckScience.Application.Photos;
using BuckScience.Web.Controllers;

namespace BuckScience.Tests;

public class PhotoFilteringIntegrationTests
{
    [Fact]
    public void BuildFilters_WithNoParameters_ReturnsNull()
    {
        // This test uses reflection to access the private BuildFilters method
        var method = typeof(PropertiesController).GetMethod("BuildFilters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = method?.Invoke(null, new object?[] 
        {
            null, null, null, null, // dates
            null, null, null, null, null, // cameras and temp
            null, null, null, null, // humidity and pressure
            null, null, null, null, // visibility and cloud cover
            null, null, null, null, // moon phase and conditions
            null, null, // pressure trends and wind directions
            null, null // timeOfDayStart and timeOfDayEnd
        });

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void BuildFilters_WithTemperatureRange_ReturnsFiltersWithTemperature()
    {
        // This test uses reflection to access the private BuildFilters method
        var method = typeof(PropertiesController).GetMethod("BuildFilters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = method?.Invoke(null, new object?[] 
        {
            null, null, null, null, // dates
            null, 10.0, 30.0, null, null, // cameras and temp
            null, null, null, null, // humidity and pressure
            null, null, null, null, // visibility and cloud cover
            null, null, null, null, // moon phase and conditions
            null, null, // pressure trends and wind directions
            null, null // timeOfDayStart and timeOfDayEnd
        }) as PhotoFilters;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10.0, result.TemperatureMin);
        Assert.Equal(30.0, result.TemperatureMax);
        Assert.True(result.HasWeatherFilters);
        Assert.True(result.HasAnyFilters);
    }

    [Fact]
    public void BuildFilters_WithCameraPlacementHistoryIds_ReturnsFiltersWithPlacementHistories()
    {
        // This test uses reflection to access the private BuildFilters method
        var method = typeof(PropertiesController).GetMethod("BuildFilters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = method?.Invoke(null, new object?[] 
        {
            null, null, null, null, // dates
            "1,2,3", null, null, null, null, // cameras (now placement history IDs) and temp
            null, null, null, null, // humidity and pressure
            null, null, null, null, // visibility and cloud cover
            null, null, null, null, // moon phase and conditions
            null, null, // pressure trends and wind directions
            null, null // timeOfDayStart and timeOfDayEnd
        }) as PhotoFilters;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CameraPlacementHistoryIds);
        Assert.Equal(3, result.CameraPlacementHistoryIds.Count);
        Assert.Contains(1, result.CameraPlacementHistoryIds);
        Assert.Contains(2, result.CameraPlacementHistoryIds);
        Assert.Contains(3, result.CameraPlacementHistoryIds);
        Assert.False(result.HasWeatherFilters);
        Assert.True(result.HasAnyFilters);
    }

    [Fact]
    public void BuildFilters_WithConditions_ReturnsFiltersWithConditions()
    {
        // This test uses reflection to access the private BuildFilters method
        var method = typeof(PropertiesController).GetMethod("BuildFilters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = method?.Invoke(null, new object?[] 
        {
            null, null, null, null, // dates
            null, null, null, null, null, // cameras and temp
            null, null, null, null, // humidity and pressure
            null, null, null, null, // visibility and cloud cover
            null, null, "Clear,Cloudy,Rainy", null, // moon phase and conditions
            null, null, // pressure trends and wind directions
            null, null // timeOfDayStart and timeOfDayEnd
        }) as PhotoFilters;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.Conditions);
        Assert.Equal(3, result.Conditions.Count);
        Assert.Contains("Clear", result.Conditions);
        Assert.Contains("Cloudy", result.Conditions);
        Assert.Contains("Rainy", result.Conditions);
        Assert.True(result.HasWeatherFilters);
        Assert.True(result.HasAnyFilters);
    }

    [Fact]
    public void BuildFilters_WithDateRange_ReturnsFiltersWithDates()
    {
        // This test uses reflection to access the private BuildFilters method
        var method = typeof(PropertiesController).GetMethod("BuildFilters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        var fromDate = DateTime.Today.AddDays(-7);
        var toDate = DateTime.Today;
        
        // Act
        var result = method?.Invoke(null, new object?[] 
        {
            fromDate, toDate, null, null, // dates
            null, null, null, null, null, // cameras and temp
            null, null, null, null, // humidity and pressure
            null, null, null, null, // visibility and cloud cover
            null, null, null, null, // moon phase and conditions
            null, null, // pressure trends and wind directions
            null, null // timeOfDayStart and timeOfDayEnd
        }) as PhotoFilters;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fromDate, result.DateTakenFrom);
        Assert.Equal(toDate, result.DateTakenTo);
        Assert.False(result.HasWeatherFilters);
        Assert.True(result.HasAnyFilters);
    }

    [Fact]
    public void BuildFilters_WithInvalidCameraPlacementHistoryIds_IgnoresInvalidIds()
    {
        // This test uses reflection to access the private BuildFilters method
        var method = typeof(PropertiesController).GetMethod("BuildFilters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act - mix of valid and invalid placement history IDs
        var result = method?.Invoke(null, new object?[] 
        {
            null, null, null, null, // dates
            "1,invalid,3,", null, null, null, null, // cameras (now placement history IDs) and temp
            null, null, null, null, // humidity and pressure
            null, null, null, null, // visibility and cloud cover
            null, null, null, null, // moon phase and conditions
            null, null, // pressure trends and wind directions
            null, null // timeOfDayStart and timeOfDayEnd
        }) as PhotoFilters;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CameraPlacementHistoryIds);
        Assert.Equal(2, result.CameraPlacementHistoryIds.Count); // Only valid IDs should be included
        Assert.Contains(1, result.CameraPlacementHistoryIds);
        Assert.Contains(3, result.CameraPlacementHistoryIds);
        Assert.DoesNotContain(0, result.CameraPlacementHistoryIds); // Invalid parse results in 0, which shouldn't be included
    }
}