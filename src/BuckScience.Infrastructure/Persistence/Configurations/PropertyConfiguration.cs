using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations
{
    public class PropertyConfiguration : IEntityTypeConfiguration<Property>
    {
        public void Configure(EntityTypeBuilder<Property> entity)
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.Name)
                  .HasMaxLength(200)
                  .IsRequired();

            // Spatial columns (SQL Server geometry)
            entity.Property(p => p.Center)
                  .HasColumnType("geometry")
                  .IsRequired();

            entity.Property(p => p.Boundary)
                  .HasColumnType("geometry");

            entity.Property(p => p.TimeZone)
                  .HasMaxLength(100)
                  .IsRequired();

            entity.Property(p => p.DayHour).IsRequired();
            entity.Property(p => p.NightHour).IsRequired();

            entity.Property(p => p.CreatedDate)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(p => p.ApplicationUserId);
            entity.HasIndex(p => new { p.ApplicationUserId, p.Name });

            // Ignore convenience, computed-at-runtime properties
            entity.Ignore(p => p.Latitude);
            entity.Ignore(p => p.Longitude);

            // Relationship configured on Camera side WithMany(p => p.Cameras)
        }
    }
}