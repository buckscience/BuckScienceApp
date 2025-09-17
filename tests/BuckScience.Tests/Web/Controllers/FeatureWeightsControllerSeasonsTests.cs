using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Domain.Enums;
using BuckScience.Web.Controllers;
using Microsoft.AspNetCore.Mvc;
using Moq;

namespace BuckScience.Tests.Web.Controllers;

public class FeatureWeightsControllerSeasonsTests
{
    private readonly Mock<IAppDbContext> _mockDb;
    private readonly Mock<ICurrentUserService> _mockCurrentUser;
    private readonly FeatureWeightsController _controller;

    public FeatureWeightsControllerSeasonsTests()
    {
        _mockDb = new Mock<IAppDbContext>();
        _mockCurrentUser = new Mock<ICurrentUserService>();
        
        _controller = new FeatureWeightsController(_mockDb.Object, _mockCurrentUser.Object);
    }

    [Fact]
    public void GetSeasons_ReturnsAllSeasonsWithDefaultMonths()
    {
        // Act
        var result = _controller.GetSeasons();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var seasons = Assert.IsAssignableFrom<IEnumerable<FeatureWeightsController.SeasonInfoResponse>>(okResult.Value);
        
        var seasonList = seasons.ToList();
        
        // Should have all 6 seasons
        Assert.Equal(6, seasonList.Count);
        
        // Check that all seasons are present
        var expectedSeasons = Enum.GetValues<Season>();
        foreach (var expectedSeason in expectedSeasons)
        {
            var seasonInfo = seasonList.First(s => s.Season == expectedSeason);
            Assert.Equal(expectedSeason.ToString(), seasonInfo.SeasonName);
            Assert.NotEmpty(seasonInfo.DefaultMonths);
            
            // Verify default months match the MonthsAttribute
            var expectedMonths = expectedSeason.GetDefaultMonths();
            Assert.Equal(expectedMonths, seasonInfo.DefaultMonths);
        }
        
        // Verify ordering by season enum value
        for (int i = 0; i < seasonList.Count - 1; i++)
        {
            Assert.True((int)seasonList[i].Season < (int)seasonList[i + 1].Season);
        }
    }

    [Fact]
    public void GetSeasons_VerifySpecificSeasonMonths()
    {
        // Act
        var result = _controller.GetSeasons();

        // Assert
        var okResult = Assert.IsType<OkObjectResult>(result);
        var seasons = Assert.IsAssignableFrom<IEnumerable<FeatureWeightsController.SeasonInfoResponse>>(okResult.Value);
        
        var seasonList = seasons.ToList();
        
        // Verify specific season month mappings
        var earlySeason = seasonList.First(s => s.Season == Season.EarlySeason);
        Assert.Equal(new[] { 9, 10 }, earlySeason.DefaultMonths);
        
        var preRut = seasonList.First(s => s.Season == Season.PreRut);
        Assert.Equal(new[] { 10 }, preRut.DefaultMonths);
        
        var rut = seasonList.First(s => s.Season == Season.Rut);
        Assert.Equal(new[] { 11 }, rut.DefaultMonths);
        
        var postRut = seasonList.First(s => s.Season == Season.PostRut);
        Assert.Equal(new[] { 12 }, postRut.DefaultMonths);
        
        var lateSeason = seasonList.First(s => s.Season == Season.LateSeason);
        Assert.Equal(new[] { 12, 1 }, lateSeason.DefaultMonths);
        
        var yearRound = seasonList.First(s => s.Season == Season.YearRound);
        Assert.Equal(new[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12 }, yearRound.DefaultMonths);
    }
}