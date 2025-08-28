using BuckScience.Web.Helpers;
using Xunit;

namespace BuckScience.Tests.Web.Helpers;

public class WeatherHelpersTests
{
    [Fact]
    public void StandardWindDirections_Contains16Directions()
    {
        // Act & Assert
        Assert.Equal(16, WeatherHelpers.StandardWindDirections.Count);
        Assert.Contains("N", WeatherHelpers.StandardWindDirections);
        Assert.Contains("NE", WeatherHelpers.StandardWindDirections);
        Assert.Contains("E", WeatherHelpers.StandardWindDirections);
        Assert.Contains("SE", WeatherHelpers.StandardWindDirections);
        Assert.Contains("S", WeatherHelpers.StandardWindDirections);
        Assert.Contains("SW", WeatherHelpers.StandardWindDirections);
        Assert.Contains("W", WeatherHelpers.StandardWindDirections);
        Assert.Contains("NW", WeatherHelpers.StandardWindDirections);
    }

    [Fact]
    public void GetWindDirectionOptions_WithAvailableDirections_SetsAvailability()
    {
        // Arrange
        var availableDirections = new[] { "N", "E", "S" };

        // Act
        var options = WeatherHelpers.GetWindDirectionOptions(availableDirections);

        // Assert
        Assert.Equal(16, options.Count);
        
        var northOption = options.First(o => o.Value == "N");
        Assert.True(northOption.IsAvailable);
        
        var eastOption = options.First(o => o.Value == "E");
        Assert.True(eastOption.IsAvailable);
        
        var westOption = options.First(o => o.Value == "W");
        Assert.False(westOption.IsAvailable);
    }

    [Fact]
    public void GetWindDirectionOptions_WithEmptyAvailable_AllUnavailable()
    {
        // Act
        var options = WeatherHelpers.GetWindDirectionOptions(Array.Empty<string>());

        // Assert
        Assert.Equal(16, options.Count);
        Assert.All(options, option => Assert.False(option.IsAvailable));
    }

    [Fact]
    public void GetWindDirectionOptions_WithNull_AllUnavailable()
    {
        // Act
        var options = WeatherHelpers.GetWindDirectionOptions(null!);

        // Assert
        Assert.Equal(16, options.Count);
        Assert.All(options, option => Assert.False(option.IsAvailable));
    }

    [Theory]
    [InlineData(0.0, "New Moon")]
    [InlineData(0.02, "New Moon")]
    [InlineData(0.1, "Waxing Crescent")]
    [InlineData(0.25, "First Quarter")]
    [InlineData(0.4, "Waxing Gibbous")]
    [InlineData(0.5, "Full Moon")]
    [InlineData(0.6, "Waning Gibbous")]
    [InlineData(0.75, "Last Quarter")]
    [InlineData(0.85, "Waning Crescent")]
    [InlineData(0.99, "New Moon")]
    [InlineData(1.0, "New Moon")]
    public void ConvertMoonPhaseToText_VariousValues_ReturnsCorrectText(double phase, string expected)
    {
        // Act
        var result = WeatherHelpers.ConvertMoonPhaseToText(phase);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("New Moon", 0.0)]
    [InlineData("Waxing Crescent", 0.125)]
    [InlineData("First Quarter", 0.25)]
    [InlineData("Waxing Gibbous", 0.375)]
    [InlineData("Full Moon", 0.5)]
    [InlineData("Waning Gibbous", 0.625)]
    [InlineData("Last Quarter", 0.75)]
    [InlineData("Waning Crescent", 0.875)]
    public void ConvertMoonPhaseTextToNumeric_ValidText_ReturnsCorrectValue(string text, double expected)
    {
        // Act
        var result = WeatherHelpers.ConvertMoonPhaseTextToNumeric(text);

        // Assert
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("Invalid Phase")]
    [InlineData("Unknown")]
    public void ConvertMoonPhaseTextToNumeric_InvalidText_ReturnsNull(string text)
    {
        // Act
        var result = WeatherHelpers.ConvertMoonPhaseTextToNumeric(text);

        // Assert
        Assert.Null(result);
    }

    [Fact]
    public void ConvertMoonPhaseTextToNumeric_CaseInsensitive_Works()
    {
        // Act & Assert
        Assert.Equal(0.5, WeatherHelpers.ConvertMoonPhaseTextToNumeric("FULL MOON"));
        Assert.Equal(0.0, WeatherHelpers.ConvertMoonPhaseTextToNumeric("new moon"));
        Assert.Equal(0.25, WeatherHelpers.ConvertMoonPhaseTextToNumeric("First Quarter"));
    }

    [Fact]
    public void WindDirectionOption_Properties_Work()
    {
        // Act
        var option = new WindDirectionOption
        {
            Value = "N",
            DisplayName = "North",
            IsAvailable = true
        };

        // Assert
        Assert.Equal("N", option.Value);
        Assert.Equal("North", option.DisplayName);
        Assert.True(option.IsAvailable);
    }
}