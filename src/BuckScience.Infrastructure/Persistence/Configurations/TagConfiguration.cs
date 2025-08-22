using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations
{
    public class TagConfiguration : IEntityTypeConfiguration<Tag>
    {
        public void Configure(EntityTypeBuilder<Tag> entity)
        {
            entity.HasKey(t => t.Id);

            entity.Property(t => t.TagName)
                  .HasMaxLength(100)
                  .IsRequired();

            // Enforce uniqueness across all tags
            entity.HasIndex(t => t.TagName).IsUnique();

            // Map isDefaultTag with a default of false; keep DB column name consistent
            entity.Property(t => t.isDefaultTag)
                  .HasColumnName("IsDefaultTag")
                  .HasDefaultValue(false)
                  .IsRequired();

            // Relationships are modeled via explicit join entities:
            // PropertyTagConfiguration and PhotoTagConfiguration handle those ends.
        }
    }
}