using BuckScience.Domain.Enums;
using BuckScience.Web.Controllers;
using System.Text.Json;
using Xunit;

namespace BuckScience.Tests.Web.Controllers;

public class SeasonOverrideRequestSerializationTests
{
    [Fact]
    public void UpdatePropertySeasonOverridesRequest_DeserializeFromJavaScriptFormat_ShouldWork()
    {
        // Arrange - This is the format that JavaScript will send after our fix
        var jsonFromJavaScript = @"{
            ""seasonOverrides"": [
                {
                    ""season"": 1,
                    ""customMonths"": [8, 9, 10],
                    ""removeOverride"": false
                },
                {
                    ""season"": 2,
                    ""customMonths"": null,
                    ""removeOverride"": true
                }
            ]
        }";

        // Act
        var request = JsonSerializer.Deserialize<PropertiesController.UpdatePropertySeasonOverridesRequest>(
            jsonFromJavaScript, 
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase });

        // Assert
        Assert.NotNull(request);
        Assert.NotNull(request.SeasonOverrides);
        Assert.Equal(2, request.SeasonOverrides.Count);

        var firstOverride = request.SeasonOverrides[0];
        Assert.Equal(Season.EarlySeason, firstOverride.Season);
        Assert.NotNull(firstOverride.CustomMonths);
        Assert.Equal(3, firstOverride.CustomMonths.Length);
        Assert.Equal(new[] { 8, 9, 10 }, firstOverride.CustomMonths);
        Assert.False(firstOverride.RemoveOverride);

        var secondOverride = request.SeasonOverrides[1];
        Assert.Equal(Season.PreRut, secondOverride.Season);
        Assert.Null(secondOverride.CustomMonths);
        Assert.True(secondOverride.RemoveOverride);
    }

    [Fact]
    public void UpdatePropertySeasonOverridesRequest_DeserializeWithStringSeasonValue_ShouldFail()
    {
        // Arrange - This is the old format that was causing the 400 error
        var jsonWithStringSeasons = @"{
            ""seasonOverrides"": [
                {
                    ""season"": ""EarlySeason"",
                    ""customMonths"": [8, 9, 10]
                }
            ]
        }";

        // Act & Assert - This should fail to deserialize
        Assert.Throws<JsonException>(() => JsonSerializer.Deserialize<PropertiesController.UpdatePropertySeasonOverridesRequest>(
            jsonWithStringSeasons, 
            new JsonSerializerOptions { PropertyNamingPolicy = JsonNamingPolicy.CamelCase }));
    }
}