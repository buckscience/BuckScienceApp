using BuckScience.Application.Profiles;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using BuckScience.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using NetTopologySuite.Geometries;

namespace BuckScience.Tests.Application.Profiles;

public class ProfileCrudTests
{
    private AppDbContext GetInMemoryDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        return new AppDbContext(options);
    }

    [Fact]
    public async Task CreateProfile_ValidData_CreatesProfile()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var user = CreateTestUser(1);
        var property = CreateTestProperty(user.Id);
        var tag = CreateTestTag();
        
        context.ApplicationUsers.Add(user);
        context.Properties.Add(property);
        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        var command = new CreateProfile.Command(
            "Test Buck",
            property.Id,
            tag.Id,
            ProfileStatus.Watching
        );

        // Act
        var profileId = await CreateProfile.HandleAsync(command, context, user.Id);

        // Assert
        var profile = await context.Profiles.FindAsync(profileId);
        Assert.NotNull(profile);
        Assert.Equal("Test Buck", profile.Name);
        Assert.Equal(property.Id, profile.PropertyId);
        Assert.Equal(tag.Id, profile.TagId);
        Assert.Equal(ProfileStatus.Watching, profile.ProfileStatus);
    }

    [Fact]
    public async Task CreateProfile_InvalidPropertyId_ThrowsUnauthorizedAccessException()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var user = CreateTestUser(1);
        var otherUser = CreateTestUser(2);
        var property = CreateTestProperty(otherUser.Id); // Property belongs to other user
        var tag = CreateTestTag();
        
        context.ApplicationUsers.AddRange(user, otherUser);
        context.Properties.Add(property);
        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        var command = new CreateProfile.Command(
            "Test Buck",
            property.Id,
            tag.Id,
            ProfileStatus.Watching
        );

        // Act & Assert
        await Assert.ThrowsAsync<UnauthorizedAccessException>(
            () => CreateProfile.HandleAsync(command, context, user.Id)
        );
    }

    [Fact]
    public async Task UpdateProfile_ValidData_UpdatesProfile()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var user = CreateTestUser(1);
        var property = CreateTestProperty(user.Id);
        var tag = CreateTestTag();
        
        context.ApplicationUsers.Add(user);
        context.Properties.Add(property);
        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        var profile = new Profile("Original Name", property.Id, tag.Id, ProfileStatus.Watching);
        context.Profiles.Add(profile);
        await context.SaveChangesAsync();

        var command = new UpdateProfile.Command(
            profile.Id,
            "Updated Name",
            ProfileStatus.HitList,
            "http://example.com/photo.jpg"
        );

        // Act
        await UpdateProfile.HandleAsync(command, context, user.Id);

        // Assert
        var updatedProfile = await context.Profiles.FindAsync(profile.Id);
        Assert.NotNull(updatedProfile);
        Assert.Equal("Updated Name", updatedProfile.Name);
        Assert.Equal(ProfileStatus.HitList, updatedProfile.ProfileStatus);
        Assert.Equal("http://example.com/photo.jpg", updatedProfile.CoverPhotoUrl);
    }

    [Fact]
    public async Task DeleteProfile_ValidId_DeletesProfile()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var user = CreateTestUser(1);
        var property = CreateTestProperty(user.Id);
        var tag = CreateTestTag();
        
        context.ApplicationUsers.Add(user);
        context.Properties.Add(property);
        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        var profile = new Profile("Test Profile", property.Id, tag.Id, ProfileStatus.Watching);
        context.Profiles.Add(profile);
        await context.SaveChangesAsync();

        var command = new DeleteProfile.Command(profile.Id);

        // Act
        await DeleteProfile.HandleAsync(command, context, user.Id);

        // Assert
        var deletedProfile = await context.Profiles.FindAsync(profile.Id);
        Assert.Null(deletedProfile);
    }

    [Fact]
    public async Task GetProfile_ValidId_ReturnsProfile()
    {
        // Arrange
        using var context = GetInMemoryDbContext();
        var user = CreateTestUser(1);
        var property = CreateTestProperty(user.Id);
        var tag = CreateTestTag();
        
        context.ApplicationUsers.Add(user);
        context.Properties.Add(property);
        context.Tags.Add(tag);
        await context.SaveChangesAsync();

        var profile = new Profile("Test Profile", property.Id, tag.Id, ProfileStatus.HitList);
        context.Profiles.Add(profile);
        await context.SaveChangesAsync();

        // Act
        var result = await GetProfile.HandleAsync(profile.Id, context, user.Id);

        // Assert
        Assert.NotNull(result);
        Assert.Equal(profile.Id, result.Id);
        Assert.Equal("Test Profile", result.Name);
        Assert.Equal(ProfileStatus.HitList, result.ProfileStatus);
        Assert.Equal(property.Name, result.PropertyName);
        Assert.Equal(tag.TagName, result.TagName);
    }

    private static ApplicationUser CreateTestUser(int id = 1)
    {
        return new ApplicationUser
        {
            Id = id,
            AzureEntraB2CId = $"test-azure-id-{id}",
            FirstName = "Test",
            LastName = "User",
            DisplayName = "Test User",
            Email = $"test{id}@example.com"
        };
    }

    private static Property CreateTestProperty(int userId)
    {
        var geometryFactory = new GeometryFactory();
        var location = geometryFactory.CreatePoint(new Coordinate(-74.0060, 40.7128));
        
        var property = new Property(
            "Test Property",
            location,
            null, // boundary
            "America/New_York",
            6, // dayHour
            18 // nightHour
        );
        property.AssignOwner(userId);
        return property;
    }

    private static Tag CreateTestTag()
    {
        return new Tag("TestTag");
    }
}