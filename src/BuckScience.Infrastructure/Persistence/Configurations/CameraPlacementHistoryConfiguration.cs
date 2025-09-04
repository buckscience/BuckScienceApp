using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations
{
    public class CameraPlacementHistoryConfiguration : IEntityTypeConfiguration<CameraPlacementHistory>
    {
        public void Configure(EntityTypeBuilder<CameraPlacementHistory> entity)
        {
            entity.HasKey(cph => cph.Id);

            entity.Property(cph => cph.CameraId).IsRequired();
            entity.Property(cph => cph.Latitude).IsRequired();
            entity.Property(cph => cph.Longitude).IsRequired();
            entity.Property(cph => cph.DirectionDegrees).IsRequired();
            entity.Property(cph => cph.StartDateTime).IsRequired();
            entity.Property(cph => cph.EndDateTime).IsRequired(false);

            // Useful indexes
            entity.HasIndex(cph => cph.CameraId);
            entity.HasIndex(cph => new { cph.CameraId, cph.EndDateTime })
                  .HasDatabaseName("IX_CameraPlacementHistory_CameraId_EndDateTime");

            // Relationships
            entity.HasOne(cph => cph.Camera)
                  .WithMany(c => c.PlacementHistories)
                  .HasForeignKey(cph => cph.CameraId)
                  .OnDelete(DeleteBehavior.Cascade);

            // Not persisted / computed
            entity.Ignore(cph => cph.Location);
            entity.Ignore(cph => cph.IsCurrentPlacement);
            entity.Ignore(cph => cph.Duration);
        }
    }
}