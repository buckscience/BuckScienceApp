using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations;

public class SubscriptionConfiguration : IEntityTypeConfiguration<Subscription>
{
    public void Configure(EntityTypeBuilder<Subscription> builder)
    {
        builder.HasKey(s => s.Id);

        builder.Property(s => s.UserId)
            .IsRequired();

        builder.Property(s => s.StripeCustomerId)
            .HasMaxLength(255);

        builder.Property(s => s.StripeSubscriptionId)
            .HasMaxLength(255);

        builder.Property(s => s.Tier)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(s => s.Status)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(s => s.CreatedAt)
            .IsRequired();

        // Relationships
        builder.HasOne(s => s.User)
            .WithMany()
            .HasForeignKey(s => s.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes and constraints
        builder.HasIndex(s => s.UserId)
            .IsUnique(); // Ensure only one subscription per user
        builder.HasIndex(s => s.StripeCustomerId);
        builder.HasIndex(s => s.StripeSubscriptionId);

        // Seed trial subscription for darrin@buckscience.com (UserId = 1)
        builder.HasData(new Subscription
        {
            Id = 1,
            UserId = 1, // Links to the seeded darrin@buckscience.com user
            StripeCustomerId = null, // No Stripe customer yet for trial
            StripeSubscriptionId = null, // No Stripe subscription yet for trial
            Tier = SubscriptionTier.Trial,
            Status = "active",
            CurrentPeriodStart = new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc), // Same as user creation date
            CurrentPeriodEnd = new DateTime(2025, 2, 3, 0, 0, 0, DateTimeKind.Utc), // 14 days trial period
            CreatedAt = new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            CanceledAt = null
        });
    }
}