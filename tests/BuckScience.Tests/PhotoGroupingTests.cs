using BuckScience.Application.Photos;
using BuckScience.Web.ViewModels.Photos;

namespace BuckScience.Tests;

public class PhotoGroupingTests
{
    [Fact]
    public void PhotoGrouping_ShouldGroupByMonthYear()
    {
        // Arrange
        var photos = new List<ListPropertyPhotos.PhotoListItem>
        {
            new(1, "url1", new DateTime(2024, 10, 15), new DateTime(2024, 10, 15), 1, "Camera1"),
            new(2, "url2", new DateTime(2024, 10, 20), new DateTime(2024, 10, 20), 1, "Camera1"),
            new(3, "url3", new DateTime(2024, 11, 5), new DateTime(2024, 11, 5), 2, "Camera2"),
            new(4, "url4", new DateTime(2025, 1, 10), new DateTime(2025, 1, 10), 1, "Camera1")
        };

        // Act
        var groups = photos.GroupByMonth();

        // Assert
        Assert.Equal(3, groups.Count);
        
        // Verify ordering (newest month first)
        Assert.Equal("January 2025", groups[0].MonthYear);
        Assert.Equal("November 2024", groups[1].MonthYear);
        Assert.Equal("October 2024", groups[2].MonthYear);
        
        // Verify counts
        Assert.Single(groups[0].Photos);
        Assert.Single(groups[1].Photos);
        Assert.Equal(2, groups[2].Photos.Count);
    }

    [Fact]
    public void CameraPhotoGrouping_ShouldGroupByMonthYear()
    {
        // Arrange
        var photos = new List<BuckScience.Application.Photos.ListCameraPhotos.PhotoListItem>
        {
            new(1, "url1", new DateTime(2024, 10, 15), new DateTime(2024, 10, 15)),
            new(2, "url2", new DateTime(2024, 10, 20), new DateTime(2024, 10, 20)),
            new(3, "url3", new DateTime(2024, 11, 5), new DateTime(2024, 11, 5))
        };

        // Act
        var groups = photos.GroupByMonth();

        // Assert
        Assert.Equal(2, groups.Count);
        
        // Verify ordering (newest month first)
        Assert.Equal("November 2024", groups[0].MonthYear);
        Assert.Equal("October 2024", groups[1].MonthYear);
        
        // Verify counts
        Assert.Single(groups[0].Photos);
        Assert.Equal(2, groups[1].Photos.Count);
    }

    [Fact]
    public void PropertyPhotosSortBy_ShouldMapCorrectly()
    {
        // Test that our enum values map correctly for sorting
        var sortByDateTakenAsc = ListPropertyPhotos.SortBy.DateTakenAsc;
        var sortByDateTakenDesc = ListPropertyPhotos.SortBy.DateTakenDesc;
        var sortByDateUploadedAsc = ListPropertyPhotos.SortBy.DateUploadedAsc;
        var sortByDateUploadedDesc = ListPropertyPhotos.SortBy.DateUploadedDesc;

        Assert.Equal(4, Enum.GetValues<ListPropertyPhotos.SortBy>().Length);
        Assert.True(Enum.IsDefined(typeof(ListPropertyPhotos.SortBy), sortByDateTakenAsc));
        Assert.True(Enum.IsDefined(typeof(ListPropertyPhotos.SortBy), sortByDateTakenDesc));
        Assert.True(Enum.IsDefined(typeof(ListPropertyPhotos.SortBy), sortByDateUploadedAsc));
        Assert.True(Enum.IsDefined(typeof(ListPropertyPhotos.SortBy), sortByDateUploadedDesc));
    }
}