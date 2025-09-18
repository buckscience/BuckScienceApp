using BuckScience.Web.Controllers;
using Xunit;

namespace BuckScience.Tests;

public class QuickFiltersTests
{
    [Fact]
    public void ApplyQuickFilter_WithDayFilter_ReturnsCorrectTimeRange()
    {
        // Arrange
        var quickFilter = "day";
        var dayHour = 6;
        var nightHour = 18;

        // Act
        var result = InvokeApplyQuickFilter(quickFilter, dayHour, nightHour);

        // Assert
        Assert.Equal(6, result.timeStart);
        Assert.Equal(18, result.timeEnd);
    }

    [Fact]
    public void ApplyQuickFilter_WithNightFilter_ReturnsCorrectTimeRange()
    {
        // Arrange
        var quickFilter = "night";
        var dayHour = 6;
        var nightHour = 18;

        // Act
        var result = InvokeApplyQuickFilter(quickFilter, dayHour, nightHour);

        // Assert
        Assert.Equal(18, result.timeStart);
        Assert.Equal(6, result.timeEnd);
    }

    [Fact]
    public void ApplyQuickFilter_WithInvalidFilter_ReturnsNull()
    {
        // Arrange
        var quickFilter = "invalid";
        var dayHour = 6;
        var nightHour = 18;

        // Act
        var result = InvokeApplyQuickFilter(quickFilter, dayHour, nightHour);

        // Assert
        Assert.Null(result.timeStart);
        Assert.Null(result.timeEnd);
    }

    [Fact]
    public void ApplyQuickFilter_WithNullFilter_ReturnsNull()
    {
        // Arrange
        string? quickFilter = null;
        var dayHour = 6;
        var nightHour = 18;

        // Act
        var result = InvokeApplyQuickFilter(quickFilter, dayHour, nightHour);

        // Assert
        Assert.Null(result.timeStart);
        Assert.Null(result.timeEnd);
    }

    [Fact]
    public void ApplyQuickFilter_WithEmptyFilter_ReturnsNull()
    {
        // Arrange
        var quickFilter = "";
        var dayHour = 6;
        var nightHour = 18;

        // Act
        var result = InvokeApplyQuickFilter(quickFilter, dayHour, nightHour);

        // Assert
        Assert.Null(result.timeStart);
        Assert.Null(result.timeEnd);
    }

    private static (int? timeStart, int? timeEnd) InvokeApplyQuickFilter(string? quickFilter, int dayHour, int nightHour)
    {
        // Use reflection to access the private ApplyQuickFilter method
        var method = typeof(PropertiesController).GetMethod("ApplyQuickFilter",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static);

        var result = method?.Invoke(null, new object?[] { quickFilter, dayHour, nightHour });
        
        if (result is ValueTuple<int?, int?> tuple)
        {
            return tuple;
        }
        
        return (null, null);
    }
}