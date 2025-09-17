using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations
{
    public class PropertySeasonMonthsOverrideConfiguration : IEntityTypeConfiguration<PropertySeasonMonthsOverride>
    {
        public void Configure(EntityTypeBuilder<PropertySeasonMonthsOverride> entity)
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.PropertyId)
                  .IsRequired();

            entity.Property(p => p.Season)
                  .IsRequired()
                  .HasConversion<int>(); // Store enum as integer

            entity.Property(p => p.MonthsJson)
                  .HasMaxLength(100) // JSON array of 12 integers shouldn't exceed 100 chars
                  .IsRequired();

            entity.Property(p => p.CreatedAt)
                  .IsRequired()
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(p => p.UpdatedAt)
                  .IsRequired();

            // Create unique index to prevent duplicate overrides for same property/season combination
            entity.HasIndex(p => new { p.PropertyId, p.Season })
                  .IsUnique()
                  .HasDatabaseName("IX_PropertySeasonMonthsOverride_Property_Season");

            // Foreign key relationship to Property
            entity.HasOne(p => p.Property)
                  .WithMany() // Property doesn't need a navigation collection for this
                  .HasForeignKey(p => p.PropertyId)
                  .OnDelete(DeleteBehavior.Cascade)
                  .IsRequired();
        }
    }
}