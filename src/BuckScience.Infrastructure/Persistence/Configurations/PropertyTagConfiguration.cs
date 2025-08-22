using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations
{
    public class PropertyTagConfiguration : IEntityTypeConfiguration<PropertyTag>
    {
        public void Configure(EntityTypeBuilder<PropertyTag> entity)
        {
            // Composite key for join table
            entity.HasKey(pt => new { pt.PropertyId, pt.TagId });

            entity.HasIndex(pt => pt.PropertyId);
            entity.HasIndex(pt => pt.TagId);

            entity.Property(pt => pt.IsFastTag)
                  .HasDefaultValue(false);

            // Join relationships
            entity.HasOne(pt => pt.Property)
                  .WithMany() // Property does not expose PropertyTags in the domain
                  .HasForeignKey(pt => pt.PropertyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pt => pt.Tag)
                  .WithMany(t => t.PropertyTags) // Tag exposes PropertyTags
                  .HasForeignKey(pt => pt.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
        }
    }
}