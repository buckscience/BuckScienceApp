using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations
{
    public class CameraConfiguration : IEntityTypeConfiguration<Camera>
    {
        public void Configure(EntityTypeBuilder<Camera> entity)
        {
            entity.HasKey(c => c.Id);

            entity.Property(c => c.Brand).HasMaxLength(100);
            entity.Property(c => c.Model).HasMaxLength(100);

            entity.Property(c => c.CreatedDate)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(c => c.IsActive)
                  .HasDefaultValue(true);

            // Useful indexes
            entity.HasIndex(c => c.PropertyId);

            // Relationships
            entity.HasOne(c => c.Property)
                  .WithMany(p => p.Cameras)
                  .HasForeignKey(c => c.PropertyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Photos)
                  .WithOne(p => p.Camera)
                  .HasForeignKey(p => p.CameraId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.PlacementHistories)
                  .WithOne(cph => cph.Camera)
                  .HasForeignKey(cph => cph.CameraId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Not persisted / computed
            entity.Ignore(c => c.PhotoCount);
            entity.Ignore(c => c.Latitude);
            entity.Ignore(c => c.Longitude);
            entity.Ignore(c => c.DirectionDegrees);
            entity.Ignore(c => c.Location);
        }
    }
}