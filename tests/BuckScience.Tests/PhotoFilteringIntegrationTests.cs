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
            null, null // pressure trends and wind directions
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
            null, null // pressure trends and wind directions
        }) as PhotoFilters;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(10.0, result.TemperatureMin);
        Assert.Equal(30.0, result.TemperatureMax);
        Assert.True(result.HasWeatherFilters);
        Assert.True(result.HasAnyFilters);
    }

    [Fact]
    public void BuildFilters_WithCameraIds_ReturnsFiltersWithCameras()
    {
        // This test uses reflection to access the private BuildFilters method
        var method = typeof(PropertiesController).GetMethod("BuildFilters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act
        var result = method?.Invoke(null, new object?[] 
        {
            null, null, null, null, // dates
            "1,2,3", null, null, null, null, // cameras and temp
            null, null, null, null, // humidity and pressure
            null, null, null, null, // visibility and cloud cover
            null, null, null, null, // moon phase and conditions
            null, null // pressure trends and wind directions
        }) as PhotoFilters;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CameraIds);
        Assert.Equal(3, result.CameraIds.Count);
        Assert.Contains(1, result.CameraIds);
        Assert.Contains(2, result.CameraIds);
        Assert.Contains(3, result.CameraIds);
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
            null, null // pressure trends and wind directions
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
            null, null // pressure trends and wind directions
        }) as PhotoFilters;

        // Assert
        Assert.NotNull(result);
        Assert.Equal(fromDate, result.DateTakenFrom);
        Assert.Equal(toDate, result.DateTakenTo);
        Assert.False(result.HasWeatherFilters);
        Assert.True(result.HasAnyFilters);
    }

    [Fact]
    public void BuildFilters_WithInvalidCameraIds_IgnoresInvalidIds()
    {
        // This test uses reflection to access the private BuildFilters method
        var method = typeof(PropertiesController).GetMethod("BuildFilters",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);
        
        // Act - mix of valid and invalid camera IDs
        var result = method?.Invoke(null, new object?[] 
        {
            null, null, null, null, // dates
            "1,invalid,3,", null, null, null, null, // cameras and temp
            null, null, null, null, // humidity and pressure
            null, null, null, null, // visibility and cloud cover
            null, null, null, null, // moon phase and conditions
            null, null // pressure trends and wind directions
        }) as PhotoFilters;

        // Assert
        Assert.NotNull(result);
        Assert.NotNull(result.CameraIds);
        Assert.Equal(2, result.CameraIds.Count); // Only valid IDs should be included
        Assert.Contains(1, result.CameraIds);
        Assert.Contains(3, result.CameraIds);
        Assert.DoesNotContain(0, result.CameraIds); // Invalid parse results in 0, which shouldn't be included
    }
}