using BuckScience.Application.Tags;
using BuckScience.Domain.Entities;
using BuckScience.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Tests;

public class TaggingSystemTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task GetOrCreateTag_ShouldCreateNewTag_WhenTagDoesNotExist()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var tagName = "newTag";

        // Act
        var result = await GetOrCreateTag.HandleAsync(tagName, context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(tagName, result.TagName);
        Assert.False(result.isDefaultTag);
        
        var tagInDb = await context.Tags.FirstOrDefaultAsync(t => t.TagName == tagName);
        Assert.NotNull(tagInDb);
    }

    [Fact]
    public async Task GetOrCreateTag_ShouldReturnExistingTag_WhenTagExists()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var existingTag = new Tag("existingTag");
        context.Tags.Add(existingTag);
        await context.SaveChangesAsync();

        // Act
        var result = await GetOrCreateTag.HandleAsync("existingTag", context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingTag.Id, result.Id);
        Assert.Equal("existingTag", result.TagName);
    }

    [Fact]
    public async Task GetOrCreateTag_ShouldBeCaseInsensitive()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var existingTag = new Tag("ExistingTag");
        context.Tags.Add(existingTag);
        await context.SaveChangesAsync();

        // Act
        var result = await GetOrCreateTag.HandleAsync("existingtag", context);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(existingTag.Id, result.Id);
    }

    [Fact]
    public void SetAsDefaultTag_ShouldSetTagAsDefault()
    {
        // Arrange
        var tag = new Tag("testTag");

        // Act
        tag.SetAsDefaultTag(true);

        // Assert
        Assert.True(tag.isDefaultTag);
    }

    [Fact]
    public async Task AssignDefaultTagsToProperty_ShouldAssignOnlyDefaultTags()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        
        var defaultTag1 = new Tag("deer");
        defaultTag1.SetAsDefaultTag(true);
        var defaultTag2 = new Tag("turkey");
        defaultTag2.SetAsDefaultTag(true);
        var regularTag = new Tag("custom");
        
        context.Tags.AddRange(defaultTag1, defaultTag2, regularTag);
        await context.SaveChangesAsync();

        var propertyId = 1;

        // Act
        await AssignDefaultTagsToProperty.HandleAsync(propertyId, context);

        // Assert
        var propertyTags = await context.PropertyTags
            .Where(pt => pt.PropertyId == propertyId)
            .ToListAsync();

        Assert.Equal(2, propertyTags.Count);
        Assert.Contains(propertyTags, pt => pt.TagId == defaultTag1.Id);
        Assert.Contains(propertyTags, pt => pt.TagId == defaultTag2.Id);
        Assert.DoesNotContain(propertyTags, pt => pt.TagId == regularTag.Id);
    }

    [Fact]
    public async Task AddTagToPhotos_ShouldNotCreateDuplicates()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        
        var tag = new Tag("testTag");
        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        // Create existing photo tag
        var existingPhotoTag = new PhotoTag(1, tag.Id);
        context.PhotoTags.Add(existingPhotoTag);
        await context.SaveChangesAsync();

        var command = new ManagePhotoTags.AddTagToPhotosCommand(new List<int> { 1, 2 }, "testTag");

        // Act
        await ManagePhotoTags.AddTagToPhotosAsync(command, context);

        // Assert
        var photoTags = await context.PhotoTags
            .Where(pt => pt.TagId == tag.Id)
            .ToListAsync();

        Assert.Equal(2, photoTags.Count); // Should have tags for photos 1 and 2, but not duplicate for photo 1
        Assert.Contains(photoTags, pt => pt.PhotoId == 1);
        Assert.Contains(photoTags, pt => pt.PhotoId == 2);
    }

    [Fact]
    public async Task RemoveTagFromPhotos_ShouldRemoveSpecifiedTags()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        
        var tag = new Tag("testTag");
        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        var photoTag1 = new PhotoTag(1, tag.Id);
        var photoTag2 = new PhotoTag(2, tag.Id);
        context.PhotoTags.AddRange(photoTag1, photoTag2);
        await context.SaveChangesAsync();

        var command = new ManagePhotoTags.RemoveTagFromPhotosCommand(new List<int> { 1 }, tag.Id);

        // Act
        await ManagePhotoTags.RemoveTagFromPhotosAsync(command, context);

        // Assert
        var remainingPhotoTags = await context.PhotoTags
            .Where(pt => pt.TagId == tag.Id)
            .ToListAsync();

        Assert.Single(remainingPhotoTags);
        Assert.Equal(2, remainingPhotoTags[0].PhotoId);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task GetOrCreateTag_ShouldThrowException_WhenTagNameIsInvalid(string invalidTagName)
    {
        // Arrange
        using var context = GetInMemoryDbContext();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(() => 
            GetOrCreateTag.HandleAsync(invalidTagName, context));
    }
}