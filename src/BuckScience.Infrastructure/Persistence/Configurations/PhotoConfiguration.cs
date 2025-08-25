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

            // Existing table structure - match user's current Photos table
            entity.Property(p => p.PhotoUrl)
                  .HasMaxLength(2048)
                  .IsRequired();

            entity.Property(p => p.DateTaken)
                  .IsRequired();

            entity.Property(p => p.DateUploaded)
                  .IsRequired();

            entity.Property(p => p.CameraId)
                  .IsRequired();

            entity.Property(p => p.WeatherId);

            // New Azure pipeline properties (optional for backward compatibility)
            entity.Property(p => p.UserId)
                  .HasMaxLength(450);

            entity.Property(p => p.ContentHash)
                  .HasMaxLength(64);

            entity.Property(p => p.ThumbBlobName)
                  .HasMaxLength(500);

            entity.Property(p => p.DisplayBlobName)
                  .HasMaxLength(500);

            entity.Property(p => p.TakenAtUtc);

            entity.Property(p => p.Latitude)
                  .HasPrecision(10, 8);

            entity.Property(p => p.Longitude)
                  .HasPrecision(11, 8);

            entity.Property(p => p.WeatherJson);

            entity.Property(p => p.Status)
                  .HasMaxLength(50);

            // Indexes for existing structure
            entity.HasIndex(p => p.CameraId);
            entity.HasIndex(p => p.DateTaken);
            entity.HasIndex(p => p.WeatherId);

            // New indexes for Azure pipeline
            entity.HasIndex(p => p.UserId);
            entity.HasIndex(p => p.ContentHash);
            entity.HasIndex(p => p.Status);

            // Unique constraint to prevent duplicate uploads (when using Azure pipeline)
            entity.HasIndex(p => new { p.UserId, p.CameraId, p.ContentHash })
                  .IsUnique()
                  .HasFilter("[UserId] IS NOT NULL AND [ContentHash] IS NOT NULL");

            // Foreign key relationships
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