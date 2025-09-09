using BuckScience.Application.Photos;
using BuckScience.Web.ViewModels.Photos;

namespace BuckScience.Tests;

public class PhotoFilteringComprehensiveTests
{
    [Fact]
    public void PhotoFiltering_BackwardCompatibility_WithoutFilters()
    {
        // This test ensures that the new filtering doesn't break existing functionality
        // when no filters are applied
        
        // Arrange
        var photos = new List<ListPropertyPhotos.PhotoListItem>
        {
            new(1, "url1", new DateTime(2024, 10, 15), new DateTime(2024, 10, 15), 1, "Camera1"),
            new(2, "url2", new DateTime(2024, 10, 20), new DateTime(2024, 10, 20), 2, "Camera2"),
            new(3, "url3", new DateTime(2024, 11, 5), new DateTime(2024, 11, 5), 1, "Camera1")
        };

        // Act - Grouping should work exactly as before when no filters are applied
        var groups = photos.GroupByMonth();

        // Assert - Should work exactly as in existing tests
        Assert.Equal(2, groups.Count);
        Assert.Equal("November 2024", groups[0].MonthYear);
        Assert.Equal("October 2024", groups[1].MonthYear);
        Assert.Single(groups[0].Photos);
        Assert.Equal(2, groups[1].Photos.Count);
    }

    [Fact]
    public void PhotoFilters_ComplexCombination_AllFilterTypes()
    {
        // Test that complex filter combinations work correctly
        
        // Arrange
        var filters = new PhotoFilters
        {
            // Date filters
            DateTakenFrom = DateTime.Today.AddDays(-30),
            DateTakenTo = DateTime.Today,
            
            // Camera filters
            CameraIds = new List<int> { 1, 3, 5 },
            
            // Weather range filters
            TemperatureMin = 10.0,
            TemperatureMax = 25.0,
            HumidityMin = 40.0,
            HumidityMax = 80.0,
            WindSpeedMax = 15.0,
            
            // Weather categorical filters
            Conditions = new List<string> { "Clear", "Partly Cloudy" },
            MoonPhaseTexts = new List<string> { "Full Moon", "New Moon" }
        };

        // Act & Assert
        Assert.True(filters.HasWeatherFilters);
        Assert.True(filters.HasAnyFilters);
        
        // Verify all filters are set correctly
        Assert.Equal(DateTime.Today.AddDays(-30), filters.DateTakenFrom);
        Assert.Equal(DateTime.Today, filters.DateTakenTo);
        Assert.Equal(3, filters.CameraIds!.Count);
        Assert.Contains(1, filters.CameraIds);
        Assert.Contains(3, filters.CameraIds);
        Assert.Contains(5, filters.CameraIds);
        Assert.Equal(10.0, filters.TemperatureMin);
        Assert.Equal(25.0, filters.TemperatureMax);
        Assert.Equal(40.0, filters.HumidityMin);
        Assert.Equal(80.0, filters.HumidityMax);
        Assert.Equal(15.0, filters.WindSpeedMax);
        Assert.Contains("Clear", filters.Conditions!);
        Assert.Contains("Partly Cloudy", filters.Conditions);
        Assert.Contains("Full Moon", filters.MoonPhaseTexts!);
        Assert.Contains("New Moon", filters.MoonPhaseTexts);
    }

    [Fact]
    public void PropertyPhotosVm_WithFilters_CorrectProperties()
    {
        // Test that the view model correctly handles filter information
        
        // Arrange
        var filters = new PhotoFilters
        {
            TemperatureMin = 15.0,
            Conditions = new List<string> { "Clear" }
        };

        var vm = new PropertyPhotosVm
        {
            PropertyId = 1,
            PropertyName = "Test Property",
            CurrentSort = "DateTakenDesc",
            TotalPhotoCount = 5,
            AppliedFilters = filters,
            AvailableCameras = new List<CameraOption>
            {
                new() { Id = 1, LocationName = "Trail Cam 1" },
                new() { Id = 2, LocationName = "Trail Cam 2" }
            },
            AvailableConditions = new List<string> { "Clear", "Cloudy", "Rainy" },
            AvailableMoonPhases = new List<string> { "Full Moon", "New Moon", "Half Moon" }
        };

        // Act & Assert
        Assert.True(vm.HasFiltersApplied);
        Assert.NotNull(vm.AppliedFilters);
        Assert.Equal(15.0, vm.AppliedFilters.TemperatureMin);
        Assert.Contains("Clear", vm.AppliedFilters.Conditions!);
        
        // Verify available options are populated
        Assert.Equal(2, vm.AvailableCameras.Count);
        Assert.Equal(3, vm.AvailableConditions.Count);
        Assert.Equal(3, vm.AvailableMoonPhases.Count);
        Assert.Equal("Trail Cam 1", vm.AvailableCameras[0].LocationName);
        Assert.Contains("Clear", vm.AvailableConditions);
        Assert.Contains("Full Moon", vm.AvailableMoonPhases);
    }

    [Fact]
    public void PropertyPhotosVm_WithoutFilters_CorrectProperties()
    {
        // Test that the view model works correctly when no filters are applied
        
        // Arrange
        var vm = new PropertyPhotosVm
        {
            PropertyId = 1,
            PropertyName = "Test Property",
            CurrentSort = "DateTakenDesc",
            TotalPhotoCount = 10,
            AppliedFilters = null
        };

        // Act & Assert
        Assert.False(vm.HasFiltersApplied);
        Assert.Null(vm.AppliedFilters);
    }

    [Fact]
    public void PhotoFilters_EdgeCases_HandleCorrectly()
    {
        // Test edge cases and boundary conditions
        
        // Test with empty lists
        var filters1 = new PhotoFilters
        {
            CameraIds = new List<int>(),
            Conditions = new List<string>()
        };
        Assert.False(filters1.HasAnyFilters); // Empty lists shouldn't count as filters
        
        // Test with null values
        var filters2 = new PhotoFilters
        {
            TemperatureMin = null,
            TemperatureMax = null,
            CameraIds = null,
            Conditions = null
        };
        Assert.False(filters2.HasAnyFilters);
        Assert.False(filters2.HasWeatherFilters);
        
        // Test with zero values (should still count as filters for numeric fields)
        var filters3 = new PhotoFilters
        {
            TemperatureMin = 0.0,
            WindSpeedMax = 0.0
        };
        Assert.True(filters3.HasAnyFilters);
        Assert.True(filters3.HasWeatherFilters);
        
        // Test moon phase with text values
        var filters4 = new PhotoFilters
        {
            MoonPhaseTexts = new List<string> { "New Moon", "Full Moon" }
        };
        Assert.True(filters4.HasWeatherFilters);
        Assert.Contains("New Moon", filters4.MoonPhaseTexts);
        Assert.Contains("Full Moon", filters4.MoonPhaseTexts);
    }

    [Fact]
    public void CameraOption_PropertiesWork()
    {
        // Test the CameraOption class
        var camera = new CameraOption
        {
            Id = 123,
            LocationName = "Test Camera"
        };
        
        Assert.Equal(123, camera.Id);
        Assert.Equal("Test Camera", camera.LocationName);
    }
}