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

            entity.Property(c => c.Name).HasMaxLength(100).IsRequired();
            entity.Property(c => c.Brand).HasMaxLength(100);
            entity.Property(c => c.Model).HasMaxLength(100);

            // Match Property's spatial approach: SQL Server geometry with SRID 4326
            entity.Property(c => c.Location)
                  .HasColumnType("geometry")
                  .IsRequired();

            entity.Property(c => c.CreatedDate)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.Property(c => c.IsActive)
                  .HasDefaultValue(true);

            // Useful indexes
            entity.HasIndex(c => c.PropertyId);
            entity.HasIndex(c => new { c.PropertyId, c.Name });

            // Relationships
            entity.HasOne(c => c.Property)
                  .WithMany(p => p.Cameras)
                  .HasForeignKey(c => c.PropertyId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(c => c.Photos)
                  .WithOne(p => p.Camera)
                  .HasForeignKey(p => p.CameraId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Not persisted / computed
            entity.Ignore(c => c.PhotoCount);
            entity.Ignore(c => c.Latitude);
            entity.Ignore(c => c.Longitude);

            // Spatial index must be created via raw SQL in a migration (optional)
            // Example:
            // migrationBuilder.Sql("CREATE SPATIAL INDEX IX_Cameras_Location ON dbo.Cameras(Location)");
        }
    }
}