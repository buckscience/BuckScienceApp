using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using BuckScience.Infrastructure.Persistence;
using BuckScience.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;

namespace BuckScience.Tests.Subscription;

public class SubscriptionServiceTests
{
    private readonly Mock<IStripeService> _mockStripeService;
    private readonly Mock<IOptions<SubscriptionSettings>> _mockSettings;
    private readonly Mock<ILogger<SubscriptionService>> _mockLogger;
    private readonly SubscriptionSettings _settings;

    public SubscriptionServiceTests()
    {
        _mockStripeService = new Mock<IStripeService>();
        _mockSettings = new Mock<IOptions<SubscriptionSettings>>();
        _mockLogger = new Mock<ILogger<SubscriptionService>>();
        
        _settings = new SubscriptionSettings
        {
            TrialDays = 14,
            Tiers = new Dictionary<string, SubscriptionLimits>
            {
                { "Trial", new SubscriptionLimits { MaxProperties = 1, MaxCameras = 2, MaxPhotos = 100 } },
                { "Fawn", new SubscriptionLimits { MaxProperties = 3, MaxCameras = 6, MaxPhotos = 500 } },
                { "Doe", new SubscriptionLimits { MaxProperties = 5, MaxCameras = 15, MaxPhotos = 2000 } }
            }
        };
        
        _mockSettings.Setup(x => x.Value).Returns(_settings);
    }

    [Fact]
    public void GetMaxProperties_ShouldReturnCorrectLimits()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new SubscriptionService(context, _mockStripeService.Object, _mockSettings.Object, _mockLogger.Object);

        // Act & Assert
        Assert.Equal(1, service.GetMaxProperties(SubscriptionTier.Trial));
        Assert.Equal(3, service.GetMaxProperties(SubscriptionTier.Fawn));
        Assert.Equal(5, service.GetMaxProperties(SubscriptionTier.Doe));
    }

    [Fact]
    public void GetMaxCameras_ShouldReturnCorrectLimits()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new SubscriptionService(context, _mockStripeService.Object, _mockSettings.Object, _mockLogger.Object);

        // Act & Assert
        Assert.Equal(2, service.GetMaxCameras(SubscriptionTier.Trial));
        Assert.Equal(6, service.GetMaxCameras(SubscriptionTier.Fawn));
        Assert.Equal(15, service.GetMaxCameras(SubscriptionTier.Doe));
    }

    [Fact]
    public void GetMaxPhotos_ShouldReturnCorrectLimits()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var service = new SubscriptionService(context, _mockStripeService.Object, _mockSettings.Object, _mockLogger.Object);

        // Act & Assert
        Assert.Equal(100, service.GetMaxPhotos(SubscriptionTier.Trial));
        Assert.Equal(500, service.GetMaxPhotos(SubscriptionTier.Fawn));
        Assert.Equal(2000, service.GetMaxPhotos(SubscriptionTier.Doe));
    }

    [Fact]
    public async Task GetUserSubscriptionTierAsync_WithNoSubscription_ShouldReturnTrial()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var user = new ApplicationUser 
        { 
            Id = 1, 
            Email = "test@example.com", 
            AzureEntraB2CId = "test-b2c-id",
            DisplayName = "Test User",
            FirstName = "Test",
            LastName = "User",
            TrialStartDate = DateTime.UtcNow.AddDays(-5) 
        };
        context.ApplicationUsers.Add(user);
        await context.SaveChangesAsync();

        var service = new SubscriptionService(context, _mockStripeService.Object, _mockSettings.Object, _mockLogger.Object);

        // Act
        var tier = await service.GetUserSubscriptionTierAsync(1);

        // Assert
        Assert.Equal(SubscriptionTier.Trial, tier);
    }

    [Fact]
    public async Task GetUserSubscriptionTierAsync_WithExpiredTrial_ShouldReturnExpired()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var user = new ApplicationUser 
        { 
            Id = 1, 
            Email = "test@example.com", 
            AzureEntraB2CId = "test-b2c-id",
            DisplayName = "Test User",
            FirstName = "Test",
            LastName = "User",
            TrialStartDate = DateTime.UtcNow.AddDays(-20) // 20 days ago, beyond 14-day trial
        };
        context.ApplicationUsers.Add(user);
        await context.SaveChangesAsync();

        var service = new SubscriptionService(context, _mockStripeService.Object, _mockSettings.Object, _mockLogger.Object);

        // Act
        var tier = await service.GetUserSubscriptionTierAsync(1);

        // Assert
        Assert.Equal(SubscriptionTier.Expired, tier);
    }

    [Fact]
    public async Task GetTrialDaysRemainingAsync_ShouldCalculateCorrectly()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var user = new ApplicationUser 
        { 
            Id = 1, 
            Email = "test@example.com", 
            AzureEntraB2CId = "test-b2c-id",
            DisplayName = "Test User",
            FirstName = "Test",
            LastName = "User",
            TrialStartDate = DateTime.UtcNow.AddDays(-5) // 5 days ago
        };
        context.ApplicationUsers.Add(user);
        await context.SaveChangesAsync();

        var service = new SubscriptionService(context, _mockStripeService.Object, _mockSettings.Object, _mockLogger.Object);

        // Act
        var daysRemaining = await service.GetTrialDaysRemainingAsync(1);

        // Assert
        Assert.True(daysRemaining >= 8 && daysRemaining <= 9); // Allow for small time differences
    }

    [Fact]
    public async Task IsTrialExpiredAsync_WithActiveTrials_ShouldReturnFalse()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var user = new ApplicationUser 
        { 
            Id = 1, 
            Email = "test@example.com", 
            AzureEntraB2CId = "test-b2c-id",
            DisplayName = "Test User",
            FirstName = "Test",
            LastName = "User",
            TrialStartDate = DateTime.UtcNow.AddDays(-5) // 5 days ago, within 14-day trial
        };
        context.ApplicationUsers.Add(user);
        await context.SaveChangesAsync();

        var service = new SubscriptionService(context, _mockStripeService.Object, _mockSettings.Object, _mockLogger.Object);

        // Act
        var isExpired = await service.IsTrialExpiredAsync(1);

        // Assert
        Assert.False(isExpired);
    }

    [Fact]
    public async Task IsTrialExpiredAsync_WithExpiredTrial_ShouldReturnTrue()
    {
        // Arrange
        var context = CreateInMemoryContext();
        var user = new ApplicationUser 
        { 
            Id = 1, 
            Email = "test@example.com", 
            AzureEntraB2CId = "test-b2c-id",
            DisplayName = "Test User",
            FirstName = "Test",
            LastName = "User",
            TrialStartDate = DateTime.UtcNow.AddDays(-20) // 20 days ago, beyond 14-day trial
        };
        context.ApplicationUsers.Add(user);
        await context.SaveChangesAsync();

        var service = new SubscriptionService(context, _mockStripeService.Object, _mockSettings.Object, _mockLogger.Object);

        // Act
        var isExpired = await service.IsTrialExpiredAsync(1);

        // Assert
        Assert.True(isExpired);
    }

    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new AppDbContext(options);
    }
}