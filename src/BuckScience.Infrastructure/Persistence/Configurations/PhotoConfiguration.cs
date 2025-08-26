using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations
{
    public class PhotoConfiguration : IEntityTypeConfiguration<Photo>
    {
        public void Configure(EntityTypeBuilder<Photo> entity)
        {
            entity.HasKey(p => p.Id);

            entity.Property(p => p.PhotoUrl)
                  .HasMaxLength(2048)
                  .IsRequired();

            entity.Property(p => p.DateTaken)
                  .IsRequired();

            entity.Property(p => p.DateUploaded)
                  .HasDefaultValueSql("GETUTCDATE()");

            entity.HasIndex(p => p.CameraId);
            entity.HasIndex(p => p.DateTaken);
            entity.HasIndex(p => p.WeatherId);

            entity.HasOne(p => p.Camera)
                  .WithMany(c => c.Photos)
                  .HasForeignKey(p => p.CameraId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(p => p.Weather)
                  .WithMany()
                  .HasForeignKey(p => p.WeatherId)
                  .OnDelete(DeleteBehavior.SetNull);

            // Using explicit join entity PhotoTag; no skip navigation on Photo to Tags.
        }
    }
}