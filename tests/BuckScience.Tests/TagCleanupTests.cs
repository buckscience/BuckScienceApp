using BuckScience.Application.Properties;
using BuckScience.Application.Tags;
using BuckScience.Domain.Entities;
using BuckScience.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Tests;

/// <summary>
/// Tests to verify that removing tags from photos properly cleans up 
/// PropertyTags and Tags tables when they are no longer in use
/// </summary>
public class TagCleanupTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task RemoveTagFromPhotos_ShouldCleanupPropertyTag_WhenNoOtherPhotosOnPropertyUseTag()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var geometryFactory = new GeometryFactory();

        // Create property with default tag
        var defaultTag = new Tag("deer");
        defaultTag.SetAsDefaultTag(true);
        context.Tags.Add(defaultTag);
        await context.SaveChangesAsync();

        var command = new CreateProperty.Command("Test Property", 40.7128, -74.0060, "America/New_York", 6, 18);
        var propertyId = await CreateProperty.HandleAsync(command, context, geometryFactory, 1, CancellationToken.None);

        // Create camera and two photos on this property
        var cameraLocation = geometryFactory.CreatePoint(new Coordinate(-74.0060, 40.7128));
        var camera = new Camera("Test Camera", "Test Brand", "Test Model", cameraLocation);
        camera.PlaceInProperty(propertyId);
        context.Cameras.Add(camera);
        await context.SaveChangesAsync();

        var photo1 = new Photo(camera.Id, "photo1.jpg", DateTime.UtcNow);
        var photo2 = new Photo(camera.Id, "photo2.jpg", DateTime.UtcNow);
        context.Photos.AddRange(photo1, photo2);
        await context.SaveChangesAsync();

        // Tag both photos with a custom tag
        var addTagCommand = new ManagePhotoTags.AddTagToPhotosCommand(
            new List<int> { photo1.Id, photo2.Id }, 
            "large_buck"
        );
        await ManagePhotoTags.AddTagToPhotosAsync(addTagCommand, context);

        // Get the tag ID for removal
        var customTag = await context.Tags.FirstAsync(t => t.TagName == "large_buck");

        // Verify initial state: property should have both deer and large_buck tags
        var initialPropertyTags = await context.PropertyTags
            .Where(pt => pt.PropertyId == propertyId)
            .Include(pt => pt.Tag)
            .Select(pt => pt.Tag.TagName)
            .OrderBy(name => name)
            .ToListAsync();
        
        Assert.Equal(2, initialPropertyTags.Count);
        Assert.Contains("deer", initialPropertyTags);
        Assert.Contains("large_buck", initialPropertyTags);

        // Act: Remove tag from one photo (tag should still be on property because photo2 still has it)
        var removeCommand1 = new ManagePhotoTags.RemoveTagFromPhotosCommand(new List<int> { photo1.Id }, customTag.Id);
        await ManagePhotoTags.RemoveTagFromPhotosAsync(removeCommand1, context);

        // Assert: PropertyTag should still exist because photo2 still has the tag
        var afterFirstRemoval = await context.PropertyTags
            .Where(pt => pt.PropertyId == propertyId && pt.TagId == customTag.Id)
            .ToListAsync();
        Assert.Single(afterFirstRemoval);

        // Act: Remove tag from the last photo (tag should be removed from property)
        var removeCommand2 = new ManagePhotoTags.RemoveTagFromPhotosCommand(new List<int> { photo2.Id }, customTag.Id);
        await ManagePhotoTags.RemoveTagFromPhotosAsync(removeCommand2, context);

        // Assert: PropertyTag should be removed now
        var afterSecondRemoval = await context.PropertyTags
            .Where(pt => pt.PropertyId == propertyId && pt.TagId == customTag.Id)
            .ToListAsync();
        Assert.Empty(afterSecondRemoval);

        // Verify only the default tag remains on the property
        var finalPropertyTags = await context.PropertyTags
            .Where(pt => pt.PropertyId == propertyId)
            .Include(pt => pt.Tag)
            .Select(pt => pt.Tag.TagName)
            .ToListAsync();
        
        Assert.Single(finalPropertyTags);
        Assert.Contains("deer", finalPropertyTags);
    }

    [Fact]
    public async Task RemoveTagFromPhotos_ShouldCleanupTag_WhenNotUsedAnywhere()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var geometryFactory = new GeometryFactory();

        // Create property
        var defaultTag = new Tag("deer");
        defaultTag.SetAsDefaultTag(true);
        context.Tags.Add(defaultTag);
        await context.SaveChangesAsync();

        var command = new CreateProperty.Command("Test Property", 40.7128, -74.0060, "America/New_York", 6, 18);
        var propertyId = await CreateProperty.HandleAsync(command, context, geometryFactory, 1, CancellationToken.None);

        // Create camera and photo
        var cameraLocation = geometryFactory.CreatePoint(new Coordinate(-74.0060, 40.7128));
        var camera = new Camera("Test Camera", "Test Brand", "Test Model", cameraLocation);
        camera.PlaceInProperty(propertyId);
        context.Cameras.Add(camera);
        await context.SaveChangesAsync();

        var photo = new Photo(camera.Id, "photo.jpg", DateTime.UtcNow);
        context.Photos.Add(photo);
        await context.SaveChangesAsync();

        // Tag photo with a custom tag
        var addTagCommand = new ManagePhotoTags.AddTagToPhotosCommand(
            new List<int> { photo.Id }, 
            "rare_albino"
        );
        await ManagePhotoTags.AddTagToPhotosAsync(addTagCommand, context);

        // Get the tag ID for removal
        var customTag = await context.Tags.FirstAsync(t => t.TagName == "rare_albino");

        // Verify tag exists
        Assert.NotNull(customTag);

        // Act: Remove tag from the only photo using it
        var removeCommand = new ManagePhotoTags.RemoveTagFromPhotosCommand(new List<int> { photo.Id }, customTag.Id);
        await ManagePhotoTags.RemoveTagFromPhotosAsync(removeCommand, context);

        // Assert: Tag should be completely removed from the system
        var tagStillExists = await context.Tags.AnyAsync(t => t.Id == customTag.Id);
        Assert.False(tagStillExists);

        // Verify default tag is not affected
        var defaultTagStillExists = await context.Tags.AnyAsync(t => t.TagName == "deer");
        Assert.True(defaultTagStillExists);
    }

    [Fact]
    public async Task RemoveTagFromPhotos_ShouldNotRemoveDefaultTag_EvenWhenUnused()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var geometryFactory = new GeometryFactory();

        // Create property with default tag
        var defaultTag = new Tag("turkey");
        defaultTag.SetAsDefaultTag(true);
        context.Tags.Add(defaultTag);
        await context.SaveChangesAsync();

        var command = new CreateProperty.Command("Test Property", 40.7128, -74.0060, "America/New_York", 6, 18);
        var propertyId = await CreateProperty.HandleAsync(command, context, geometryFactory, 1, CancellationToken.None);

        // Create camera and photo
        var cameraLocation = geometryFactory.CreatePoint(new Coordinate(-74.0060, 40.7128));
        var camera = new Camera("Test Camera", "Test Brand", "Test Model", cameraLocation);
        camera.PlaceInProperty(propertyId);
        context.Cameras.Add(camera);
        await context.SaveChangesAsync();

        var photo = new Photo(camera.Id, "photo.jpg", DateTime.UtcNow);
        context.Photos.Add(photo);
        await context.SaveChangesAsync();

        // Manually add the default tag to the photo
        var photoTag = new PhotoTag(photo.Id, defaultTag.Id);
        context.PhotoTags.Add(photoTag);
        await context.SaveChangesAsync();

        // Act: Remove the default tag from the photo
        var removeCommand = new ManagePhotoTags.RemoveTagFromPhotosCommand(new List<int> { photo.Id }, defaultTag.Id);
        await ManagePhotoTags.RemoveTagFromPhotosAsync(removeCommand, context);

        // Assert: Default tag should still exist even though it's not used
        var tagStillExists = await context.Tags.AnyAsync(t => t.Id == defaultTag.Id);
        Assert.True(tagStillExists);

        // PropertyTag should be removed though
        var propertyTagExists = await context.PropertyTags.AnyAsync(pt => pt.PropertyId == propertyId && pt.TagId == defaultTag.Id);
        Assert.False(propertyTagExists);
    }

    [Fact]
    public async Task RemoveTagFromPhotos_ShouldKeepTagAndPropertyTag_WhenUsedOnOtherProperties()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var geometryFactory = new GeometryFactory();

        // Create two properties
        var defaultTag = new Tag("deer");
        defaultTag.SetAsDefaultTag(true);
        context.Tags.Add(defaultTag);
        await context.SaveChangesAsync();

        var command1 = new CreateProperty.Command("Property 1", 40.7128, -74.0060, "America/New_York", 6, 18);
        var property1Id = await CreateProperty.HandleAsync(command1, context, geometryFactory, 1, CancellationToken.None);

        var command2 = new CreateProperty.Command("Property 2", 41.7128, -75.0060, "America/New_York", 6, 18);
        var property2Id = await CreateProperty.HandleAsync(command2, context, geometryFactory, 1, CancellationToken.None);

        // Create cameras and photos on both properties
        var camera1Location = geometryFactory.CreatePoint(new Coordinate(-74.0060, 40.7128));
        var camera1 = new Camera("Camera 1", "Brand", "Model", camera1Location);
        camera1.PlaceInProperty(property1Id);

        var camera2Location = geometryFactory.CreatePoint(new Coordinate(-75.0060, 41.7128));
        var camera2 = new Camera("Camera 2", "Brand", "Model", camera2Location);
        camera2.PlaceInProperty(property2Id);

        context.Cameras.AddRange(camera1, camera2);
        await context.SaveChangesAsync();

        var photo1 = new Photo(camera1.Id, "photo1.jpg", DateTime.UtcNow);
        var photo2 = new Photo(camera2.Id, "photo2.jpg", DateTime.UtcNow);
        context.Photos.AddRange(photo1, photo2);
        await context.SaveChangesAsync();

        // Tag both photos with the same custom tag
        var addTagCommand = new ManagePhotoTags.AddTagToPhotosCommand(
            new List<int> { photo1.Id, photo2.Id }, 
            "shared_tag"
        );
        await ManagePhotoTags.AddTagToPhotosAsync(addTagCommand, context);

        var customTag = await context.Tags.FirstAsync(t => t.TagName == "shared_tag");

        // Act: Remove tag from photo1 only
        var removeCommand = new ManagePhotoTags.RemoveTagFromPhotosCommand(new List<int> { photo1.Id }, customTag.Id);
        await ManagePhotoTags.RemoveTagFromPhotosAsync(removeCommand, context);

        // Assert: Tag should still exist because it's used on property2
        var tagStillExists = await context.Tags.AnyAsync(t => t.Id == customTag.Id);
        Assert.True(tagStillExists);

        // PropertyTag for property1 should be removed
        var property1TagExists = await context.PropertyTags.AnyAsync(pt => pt.PropertyId == property1Id && pt.TagId == customTag.Id);
        Assert.False(property1TagExists);

        // PropertyTag for property2 should still exist
        var property2TagExists = await context.PropertyTags.AnyAsync(pt => pt.PropertyId == property2Id && pt.TagId == customTag.Id);
        Assert.True(property2TagExists);
    }
}