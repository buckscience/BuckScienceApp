using BuckScience.Domain.Enums;

namespace BuckScience.Domain.Entities;

public class Subscription
{
    public int Id { get; set; }
    public int UserId { get; set; }
    public string? StripeCustomerId { get; set; }
    public string? StripeSubscriptionId { get; set; }
    public SubscriptionTier Tier { get; set; } = SubscriptionTier.Trial;
    public string Status { get; set; } = "active"; // active, canceled, past_due, etc.
    public DateTime? CurrentPeriodStart { get; set; }
    public DateTime? CurrentPeriodEnd { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? CanceledAt { get; set; }

    // Navigation property
    public ApplicationUser User { get; set; } = null!;
}