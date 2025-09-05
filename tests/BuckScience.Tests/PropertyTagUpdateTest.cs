using BuckScience.Application.Properties;
using BuckScience.Application.Tags;
using BuckScience.Domain.Entities;
using BuckScience.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Tests;

/// <summary>
/// Test to demonstrate that tagging photos now properly updates property tags
/// </summary>
public class PropertyTagUpdateTest
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task TaggingPhoto_ShouldUpdatePropertyTags()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var geometryFactory = new GeometryFactory();

        // Create a property (this will auto-assign default tags)
        var defaultTag = new Tag("deer");
        defaultTag.SetAsDefaultTag(true);
        context.Tags.Add(defaultTag);
        await context.SaveChangesAsync();

        var command = new CreateProperty.Command("Test Property", 40.7128, -74.0060, "America/New_York", 6, 18);
        var propertyId = await CreateProperty.HandleAsync(command, context, geometryFactory, 1, CancellationToken.None);

        // Create a camera and photo on this property
        var cameraLocation = geometryFactory.CreatePoint(new Coordinate(-74.0060, 40.7128));
        var camera = new Camera("Test Camera", "Test Brand", "Test Model");
        camera.PlaceInProperty(propertyId);
        camera.PlaceAt(40.7128, -74.0060, 0f, DateTime.UtcNow);
        context.Cameras.Add(camera);
        await context.SaveChangesAsync();

        var photo = new Photo(camera.Id, "test-photo.jpg", DateTime.UtcNow);
        context.Photos.Add(photo);
        await context.SaveChangesAsync();

        // Verify initial state - property should only have the default "deer" tag
        var initialPropertyTags = await context.PropertyTags
            .Where(pt => pt.PropertyId == propertyId)
            .Include(pt => pt.Tag)
            .ToListAsync();
        
        Assert.Single(initialPropertyTags);
        Assert.Equal("deer", initialPropertyTags[0].Tag.TagName);

        // Act: Tag the photo with a new tag
        var addTagCommand = new ManagePhotoTags.AddTagToPhotosCommand(
            new List<int> { photo.Id }, 
            "large_buck"
        );
        await ManagePhotoTags.AddTagToPhotosAsync(addTagCommand, context);

        // Assert: Property should now have both tags
        var finalPropertyTags = await context.PropertyTags
            .Where(pt => pt.PropertyId == propertyId)
            .Include(pt => pt.Tag)
            .Select(pt => pt.Tag.TagName)
            .OrderBy(name => name)
            .ToListAsync();

        Assert.Equal(2, finalPropertyTags.Count);
        Assert.Contains("deer", finalPropertyTags);
        Assert.Contains("large_buck", finalPropertyTags);

        // Verify the photo is properly tagged
        var photoTags = await ManagePhotoTags.GetPhotoTagsAsync(photo.Id, context);
        Assert.Single(photoTags);
        Assert.Equal("large_buck", photoTags[0].Name);

        // Verify property tags are available through the API
        var availableTags = await ManagePhotoTags.GetAvailableTagsForPropertyAsync(propertyId, context);
        Assert.Equal(2, availableTags.Count);
        Assert.Contains(availableTags, t => t.Name == "deer");
        Assert.Contains(availableTags, t => t.Name == "large_buck");
    }
}