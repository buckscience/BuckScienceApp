using BuckScience.Application.Properties;
using BuckScience.Application.Tags;
using BuckScience.Domain.Entities;
using BuckScience.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Tests;

public class TaggingIntegrationTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateProperty_ShouldAutoAssignDefaultTags()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var geometryFactory = new GeometryFactory();

        // Seed default tags
        var defaultTags = new[]
        {
            "deer", "turkey", "bear", "buck", "doe", "predator"
        };

        foreach (var tagName in defaultTags)
        {
            var tag = new Tag(tagName);
            tag.SetAsDefaultTag(true);
            context.Tags.Add(tag);
        }
        await context.SaveChangesAsync();

        var command = new CreateProperty.Command(
            "Test Property",
            40.7128, // Latitude
            -74.0060, // Longitude
            "America/New_York",
            6, // DayHour
            18  // NightHour
        );

        // Act
        var propertyId = await CreateProperty.HandleAsync(command, context, geometryFactory, 1, CancellationToken.None);

        // Assert
        var property = await context.Properties.FindAsync(propertyId);
        Assert.NotNull(property);

        var propertyTags = await context.PropertyTags
            .Where(pt => pt.PropertyId == propertyId)
            .Include(pt => pt.Tag)
            .ToListAsync();

        Assert.Equal(6, propertyTags.Count);
        var assignedTagNames = propertyTags.Select(pt => pt.Tag.TagName).ToList();
        foreach (var expectedTag in defaultTags)
        {
            Assert.Contains(expectedTag, assignedTagNames);
        }
    }

    [Fact]
    public async Task EndToEndTaggingWorkflow_ShouldWorkCorrectly()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var geometryFactory = new GeometryFactory();

        // Create a property with default tags
        var defaultTag = new Tag("deer");
        defaultTag.SetAsDefaultTag(true);
        context.Tags.Add(defaultTag);
        await context.SaveChangesAsync();

        var command = new CreateProperty.Command("Test Property", 40.7128, -74.0060, "America/New_York", 6, 18);
        var propertyId = await CreateProperty.HandleAsync(command, context, geometryFactory, 1, CancellationToken.None);

        // Create dummy camera for testing
        var cameraLocation = geometryFactory.CreatePoint(new Coordinate(-74.0060, 40.7128));
        var camera = new Camera("Test Camera", "Test Brand", "Test Model");
        camera.PlaceInProperty(propertyId);
        camera.PlaceAt(40.7128, -74.0060, 0f, DateTime.UtcNow);
        context.Cameras.Add(camera);
        await context.SaveChangesAsync();

        // Simulate photo creation (normally done by UploadPhotos)
        var photo1 = new Photo(camera.Id, "test-url-1.jpg", DateTime.UtcNow);
        var photo2 = new Photo(camera.Id, "test-url-2.jpg", DateTime.UtcNow);
        context.Photos.AddRange(photo1, photo2);
        await context.SaveChangesAsync();

        // Act 1: Add a custom tag to photos
        var addTagCommand = new ManagePhotoTags.AddTagToPhotosCommand(
            new List<int> { photo1.Id, photo2.Id }, 
            "large_buck"
        );
        await ManagePhotoTags.AddTagToPhotosAsync(addTagCommand, context);

        // Act 2: Add default tag to one photo
        var addDefaultTagCommand = new ManagePhotoTags.AddTagToPhotosCommand(
            new List<int> { photo1.Id }, 
            "deer"
        );
        await ManagePhotoTags.AddTagToPhotosAsync(addDefaultTagCommand, context);

        // Act 3: Get available tags for property
        var availableTags = await ManagePhotoTags.GetAvailableTagsForPropertyAsync(propertyId, context);

        // Act 4: Get tags for specific photo
        var photo1Tags = await ManagePhotoTags.GetPhotoTagsAsync(photo1.Id, context);

        // Assert
        // Should have both the default tag and the custom tag available for property 
        // (deer was added during property creation, large_buck was added when photos were tagged)
        Assert.Equal(2, availableTags.Count);
        Assert.Contains(availableTags, t => t.Name == "deer");
        Assert.Contains(availableTags, t => t.Name == "large_buck");

        // Photo 1 should have both tags
        Assert.Equal(2, photo1Tags.Count);
        Assert.Contains(photo1Tags, t => t.Name == "deer");
        Assert.Contains(photo1Tags, t => t.Name == "large_buck");

        // Photo 2 should have only the custom tag
        var photo2Tags = await ManagePhotoTags.GetPhotoTagsAsync(photo2.Id, context);
        Assert.Single(photo2Tags);
        Assert.Equal("large_buck", photo2Tags[0].Name);

        // Act 5: Remove tag from photo
        var customTag = await context.Tags.FirstAsync(t => t.TagName == "large_buck");
        var removeTagCommand = new ManagePhotoTags.RemoveTagFromPhotosCommand(
            new List<int> { photo1.Id }, 
            customTag.Id
        );
        await ManagePhotoTags.RemoveTagFromPhotosAsync(removeTagCommand, context);

        // Final Assert: Photo 1 should now have only the deer tag
        var finalPhoto1Tags = await ManagePhotoTags.GetPhotoTagsAsync(photo1.Id, context);
        Assert.Single(finalPhoto1Tags);
        Assert.Equal("deer", finalPhoto1Tags[0].Name);
    }

    [Fact]
    public async Task TagUniqueness_ShouldBeEnforcedCorrectly()
    {
        // Arrange
        using var context = GetInMemoryDbContext();

        // Act: Try to create multiple tags with same name (case insensitive)
        var tag1 = await GetOrCreateTag.HandleAsync("Deer", context);
        var tag2 = await GetOrCreateTag.HandleAsync("deer", context);
        var tag3 = await GetOrCreateTag.HandleAsync("DEER", context);

        // Assert: All should return the same tag instance
        Assert.Equal(tag1.Id, tag2.Id);
        Assert.Equal(tag1.Id, tag3.Id);
        
        var totalTags = await context.Tags.CountAsync();
        Assert.Equal(1, totalTags);
    }
}