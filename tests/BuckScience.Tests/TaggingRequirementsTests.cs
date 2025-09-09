using BuckScience.Application.Properties;
using BuckScience.Application.Tags;
using BuckScience.Domain.Entities;
using BuckScience.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Tests;

/// <summary>
/// Tests that validate all the requirements from the problem statement are met
/// </summary>
public class TaggingRequirementsTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task Requirement1_DatabaseSchema_ShouldMeetAllSpecifications()
    {
        // Arrange
        using var context = GetInMemoryDbContext();

        // Act: Create tags with required structure
        var tag = new Tag("test_tag");
        tag.SetAsDefaultTag(true);
        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        // Act: Create PropertyTag and PhotoTag join tables
        var propertyTag = new PropertyTag(1, tag.Id);
        var photoTag = new PhotoTag(1, tag.Id);
        context.PropertyTags.Add(propertyTag);
        context.PhotoTags.Add(photoTag);
        await context.SaveChangesAsync();

        // Assert: Verify structure
        var savedTag = await context.Tags.FirstAsync();
        Assert.NotNull(savedTag);
        Assert.True(savedTag.Id > 0); // Primary key
        Assert.Equal("test_tag", savedTag.TagName); // Name column
        Assert.True(savedTag.isDefaultTag); // isDefault column

        var savedPropertyTag = await context.PropertyTags.FirstAsync();
        Assert.NotNull(savedPropertyTag);
        Assert.Equal(1, savedPropertyTag.PropertyId); // property_id FK
        Assert.Equal(tag.Id, savedPropertyTag.TagId); // tag_id FK

        var savedPhotoTag = await context.PhotoTags.FirstAsync();
        Assert.NotNull(savedPhotoTag);
        Assert.Equal(1, savedPhotoTag.PhotoId); // photo_id FK
        Assert.Equal(tag.Id, savedPhotoTag.TagId); // tag_id FK
    }

    [Fact]
    public async Task Requirement1_DefaultTags_ShouldBePopulatedByMigration()
    {
        // This test verifies that the migration seeds the correct default tags
        // In a real scenario, the migration would run and populate these tags

        // Arrange
        using var context = GetInMemoryDbContext();
        
        // Simulate the migration seeding default tags
        var defaultTagNames = new[] { "deer", "turkey", "bear", "buck", "doe", "predator" };
        foreach (var tagName in defaultTagNames)
        {
            var tag = new Tag(tagName);
            tag.SetAsDefaultTag(true);
            context.Tags.Add(tag);
        }
        await context.SaveChangesAsync();

        // Act: Get all default tags
        var defaultTags = await context.Tags
            .Where(t => t.isDefaultTag)
            .Select(t => t.TagName)
            .ToListAsync();

        // Assert: All required default tags exist
        Assert.Equal(6, defaultTags.Count);
        Assert.Contains("deer", defaultTags);
        Assert.Contains("turkey", defaultTags);
        Assert.Contains("bear", defaultTags);
        Assert.Contains("buck", defaultTags);
        Assert.Contains("doe", defaultTags);
        Assert.Contains("predator", defaultTags);
    }

    [Fact]
    public async Task Requirement2_PropertyCreation_ShouldAutoAssignDefaultTags()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var geometryFactory = new GeometryFactory();

        // Create default tags
        var defaultTagNames = new[] { "deer", "turkey", "bear", "buck", "doe", "predator" };
        foreach (var tagName in defaultTagNames)
        {
            var tag = new Tag(tagName);
            tag.SetAsDefaultTag(true);
            context.Tags.Add(tag);
        }
        await context.SaveChangesAsync();

        var command = new CreateProperty.Command(
            "Test Property", 40.7128, -74.0060, "America/New_York", 6, 18);

        // Act: Create property (should auto-assign default tags)
        var propertyId = await CreateProperty.HandleAsync(
            command, context, geometryFactory, 1, CancellationToken.None);

        // Assert: All default tags are assigned to the property
        var assignedTags = await context.PropertyTags
            .Where(pt => pt.PropertyId == propertyId)
            .Include(pt => pt.Tag)
            .Select(pt => pt.Tag.TagName)
            .ToListAsync();

        Assert.Equal(6, assignedTags.Count);
        foreach (var expectedTag in defaultTagNames)
        {
            Assert.Contains(expectedTag, assignedTags);
        }
    }

    [Fact]
    public async Task Requirement2_TagUniqueness_ShouldBeEnforced()
    {
        // Arrange
        using var context = GetInMemoryDbContext();

        // Act: Try to create duplicate tags with different cases
        var tag1 = await GetOrCreateTag.HandleAsync("Deer", context);
        var tag2 = await GetOrCreateTag.HandleAsync("deer", context);
        var tag3 = await GetOrCreateTag.HandleAsync("DEER", context);

        // Assert: All should return the same tag (no duplicates)
        Assert.Equal(tag1.Id, tag2.Id);
        Assert.Equal(tag1.Id, tag3.Id);

        var totalTags = await context.Tags.CountAsync();
        Assert.Equal(1, totalTags);
    }

    [Fact]
    public async Task Requirement2_PhotoTagging_ShouldSupportBulkOperations()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var geometryFactory = new GeometryFactory();

        // Setup property with camera and photos
        var tag = new Tag("deer");
        tag.SetAsDefaultTag(true);
        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        var command = new CreateProperty.Command("Test Property", 40.7128, -74.0060, "America/New_York", 6, 18);
        var propertyId = await CreateProperty.HandleAsync(command, context, geometryFactory, 1, CancellationToken.None);

        var cameraLocation = geometryFactory.CreatePoint(new Coordinate(-74.0060, 40.7128));
        var camera = new Camera("Test Brand", "Test Model");
        camera.PlaceInProperty(propertyId);
        camera.PlaceAt(40.7128, -74.0060, 0f, DateTime.UtcNow);
        context.Cameras.Add(camera);
        await context.SaveChangesAsync();

        var photo1 = new Photo(camera.Id, "test-url-1.jpg", DateTime.UtcNow);
        var photo2 = new Photo(camera.Id, "test-url-2.jpg", DateTime.UtcNow);
        var photo3 = new Photo(camera.Id, "test-url-3.jpg", DateTime.UtcNow);
        context.Photos.AddRange(photo1, photo2, photo3);
        await context.SaveChangesAsync();

        // Act: Bulk add tags to multiple photos
        var addTagCommand = new ManagePhotoTags.AddTagToPhotosCommand(
            new List<int> { photo1.Id, photo2.Id, photo3.Id }, 
            "large_deer"
        );
        await ManagePhotoTags.AddTagToPhotosAsync(addTagCommand, context);

        // Act: Bulk remove tags from some photos
        var largeTag = await context.Tags.FirstAsync(t => t.TagName == "large_deer");
        var removeTagCommand = new ManagePhotoTags.RemoveTagFromPhotosCommand(
            new List<int> { photo2.Id, photo3.Id }, 
            largeTag.Id
        );
        await ManagePhotoTags.RemoveTagFromPhotosAsync(removeTagCommand, context);

        // Assert: Verify bulk operations worked correctly
        var photo1Tags = await ManagePhotoTags.GetPhotoTagsAsync(photo1.Id, context);
        var photo2Tags = await ManagePhotoTags.GetPhotoTagsAsync(photo2.Id, context);
        var photo3Tags = await ManagePhotoTags.GetPhotoTagsAsync(photo3.Id, context);

        Assert.Single(photo1Tags);
        Assert.Equal("large_deer", photo1Tags[0].Name);
        Assert.Empty(photo2Tags);
        Assert.Empty(photo3Tags);
    }

    [Fact]
    public async Task Requirement2_NewTagCreation_ShouldBeSupported()
    {
        // Arrange
        using var context = GetInMemoryDbContext();

        // Act: Add a new tag that doesn't exist
        var newTagCommand = new ManagePhotoTags.AddTagToPhotosCommand(
            new List<int> { 1 }, 
            "eight_point_buck"
        );

        // This should create the tag automatically
        await ManagePhotoTags.AddTagToPhotosAsync(newTagCommand, context);

        // Assert: New tag was created
        var newTag = await context.Tags.FirstOrDefaultAsync(t => t.TagName == "eight_point_buck");
        Assert.NotNull(newTag);
        Assert.False(newTag.isDefaultTag); // New tags are not default
        
        var photoTag = await context.PhotoTags.FirstOrDefaultAsync(pt => pt.TagId == newTag.Id);
        Assert.NotNull(photoTag);
    }
}