using BuckScience.Domain.Enums;
using BuckScience.Web.Controllers;
using Microsoft.AspNetCore.Mvc;

namespace BuckScience.Tests.Web.Controllers;

public class FeatureWeightsControllerBasicTests
{
    [Fact]
    public void GetSeasons_ReturnsAllSeasonsWithCorrectData()
    {
        // Arrange
        var controller = new FeatureWeightsController(null!, null!);

        // Act
        var result = controller.GetSeasons();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var seasons = Assert.IsAssignableFrom<IEnumerable<FeatureWeightsController.SeasonInfoResponse>>(okResult.Value);
        
        var seasonList = seasons.ToList();
        
        // Should have all 6 seasons
        Assert.Equal(6, seasonList.Count);
        
        // Verify EarlySeason has correct default months
        var earlySeason = seasonList.First(s => s.Season == Season.EarlySeason);
        Assert.Equal(Season.EarlySeason, earlySeason.Season);
        Assert.Equal("EarlySeason", earlySeason.SeasonName);
        Assert.Equal(new[] { 9, 10 }, earlySeason.DefaultMonths);
        
        // Verify YearRound has all months
        var yearRound = seasonList.First(s => s.Season == Season.YearRound);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, yearRound.DefaultMonths);
    }

    [Fact]
    public void PropertiesController_DTOs_HaveCorrectStructure()
    {
        // Test that our DTOs have the expected properties
        var seasonOverrideResponse = new PropertiesController.SeasonOverrideResponse
        {
            Season = Season.PreRut,
            SeasonName = "PreRut",
            DefaultMonths = new[] { 10 },
            CustomMonths = new[] { 9, 10 },
            HasOverride = true
        };

        Assert.Equal(Season.PreRut, seasonOverrideResponse.Season);
        Assert.Equal("PreRut", seasonOverrideResponse.SeasonName);
        Assert.True(seasonOverrideResponse.HasOverride);
        Assert.Equal(new[] { 10 }, seasonOverrideResponse.DefaultMonths);
        Assert.Equal(new[] { 9, 10 }, seasonOverrideResponse.CustomMonths);

        var updateRequest = new PropertiesController.SeasonOverrideUpdateRequest
        {
            Season = Season.Rut,
            CustomMonths = new[] { 11, 12 },
            RemoveOverride = false
        };

        Assert.Equal(Season.Rut, updateRequest.Season);
        Assert.Equal(new[] { 11, 12 }, updateRequest.CustomMonths);
        Assert.False(updateRequest.RemoveOverride);
    }
}