using BuckScience.Application.Abstractions;
using BuckScience.Application.Abstractions.Auth;
using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using BuckScience.Infrastructure.Persistence;
using BuckScience.Web.Controllers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Stripe;
using System.Text;
using System.Text.Json;

namespace BuckScience.Tests.Subscription;

public class SubscriptionWebhookTests
{
    private readonly Mock<ISubscriptionService> _mockSubscriptionService;
    private readonly Mock<ICurrentUserService> _mockCurrentUserService;
    private readonly Mock<IStripeService> _mockStripeService;
    private readonly Mock<ILogger<SubscriptionController>> _mockLogger;
    private readonly SubscriptionController _controller;
    private readonly AppDbContext _context;

    public SubscriptionWebhookTests()
    {
        _mockSubscriptionService = new Mock<ISubscriptionService>();
        _mockCurrentUserService = new Mock<ICurrentUserService>();
        _mockStripeService = new Mock<IStripeService>();
        _mockLogger = new Mock<ILogger<SubscriptionController>>();
        
        _context = CreateInMemoryContext();
        
        _controller = new SubscriptionController(
            _mockSubscriptionService.Object,
            _mockCurrentUserService.Object,
            _mockStripeService.Object,
            _context,
            _mockLogger.Object);
    }

    [Fact]
    public async Task Webhook_WithCheckoutSessionCompleted_UpdatesSubscriptionStatus()
    {
        // Arrange
        var customerId = "cus_test123";
        var subscriptionId = "sub_test123";
        
        // Create a test user and subscription
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
        
        var subscription = new BuckScience.Domain.Entities.Subscription
        {
            Id = 1,
            UserId = 1,
            StripeCustomerId = customerId,
            Tier = SubscriptionTier.Trial,
            Status = "active",
            User = user
        };
        
        _context.ApplicationUsers.Add(user);
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Create webhook event JSON
        var webhookEvent = new
        {
            id = "evt_test123",
            type = "checkout.session.completed",
            data = new
            {
                @object = new
                {
                    id = "cs_test123",
                    customer = customerId,
                    mode = "subscription",
                    subscription = subscriptionId
                }
            }
        };

        var json = JsonSerializer.Serialize(webhookEvent);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = stream;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.Webhook();

        // Assert
        Assert.IsType<OkResult>(result);
        
        // Verify subscription was updated
        var updatedSubscription = await _context.Subscriptions.FindAsync(1);
        Assert.NotNull(updatedSubscription);
        Assert.Equal(subscriptionId, updatedSubscription.StripeSubscriptionId);
        Assert.Equal("active", updatedSubscription.Status);
    }

    [Fact]
    public async Task Webhook_WithSubscriptionDeleted_CancelsSubscription()
    {
        // Arrange
        var stripeSubscriptionId = "sub_test123";
        
        var user = new ApplicationUser
        {
            Id = 1,
            Email = "test@example.com",
            AzureEntraB2CId = "test-b2c-id",
            DisplayName = "Test User",
            FirstName = "Test",
            LastName = "User"
        };
        
        var subscription = new BuckScience.Domain.Entities.Subscription
        {
            Id = 1,
            UserId = 1,
            StripeSubscriptionId = stripeSubscriptionId,
            Tier = SubscriptionTier.Fawn,
            Status = "active",
            User = user
        };
        
        _context.ApplicationUsers.Add(user);
        _context.Subscriptions.Add(subscription);
        await _context.SaveChangesAsync();

        // Create webhook event JSON
        var webhookEvent = new
        {
            id = "evt_test123",
            type = "customer.subscription.deleted",
            data = new
            {
                @object = new
                {
                    id = stripeSubscriptionId,
                    status = "canceled"
                }
            }
        };

        var json = JsonSerializer.Serialize(webhookEvent);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = stream;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.Webhook();

        // Assert
        Assert.IsType<OkResult>(result);
        
        // Verify subscription was canceled
        var updatedSubscription = await _context.Subscriptions.FindAsync(1);
        Assert.NotNull(updatedSubscription);
        Assert.Equal("canceled", updatedSubscription.Status);
        Assert.NotNull(updatedSubscription.CanceledAt);
    }

    [Fact]
    public async Task Webhook_WithInvalidJSON_ReturnsServerError()
    {
        // Arrange
        var invalidJson = "{ invalid json }";
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(invalidJson));
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = stream;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.Webhook();

        // Assert
        var statusResult = Assert.IsType<StatusCodeResult>(result);
        Assert.Equal(500, statusResult.StatusCode);
    }

    [Fact]
    public async Task Webhook_WithUnhandledEventType_ReturnsOk()
    {
        // Arrange
        var webhookEvent = new
        {
            id = "evt_test123",
            type = "payment_intent.succeeded",
            data = new
            {
                @object = new
                {
                    id = "pi_test123"
                }
            }
        };

        var json = JsonSerializer.Serialize(webhookEvent);
        var stream = new MemoryStream(Encoding.UTF8.GetBytes(json));
        
        var httpContext = new DefaultHttpContext();
        httpContext.Request.Body = stream;
        _controller.ControllerContext = new ControllerContext { HttpContext = httpContext };

        // Act
        var result = await _controller.Webhook();

        // Assert
        Assert.IsType<OkResult>(result);
    }

    private static AppDbContext CreateInMemoryContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        
        return new AppDbContext(options);
    }
}