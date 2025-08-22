using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations
{
    public class ProfileConfiguration : IEntityTypeConfiguration<Profile>
    {
        public void Configure(EntityTypeBuilder<Profile> entity)
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                  .HasMaxLength(100)
                  .IsRequired();

            // Enum maps to int by default
            entity.Property(p => p.ProfileStatus)
                  .IsRequired();

            // Indexes to help typical lookups
            entity.HasIndex(p => p.PropertyId);
            entity.HasIndex(p => p.TagId);
            entity.HasIndex(p => new { p.PropertyId, p.TagId });

            // Relationships
            entity.HasOne(p => p.Property)
                  .WithMany() // Property does not expose Profiles navigation
                  .HasForeignKey(p => p.PropertyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Tag)
                  .WithMany() // Tag does not expose Profiles navigation
                  .HasForeignKey(p => p.TagId)
                  .OnDelete(DeleteBehavior.Restrict); // prevent accidental cascading via shared tags

            // No mapping for UI-only fields like PhotoUrl (keep those in Web models/DTOs)
        }
    }
}