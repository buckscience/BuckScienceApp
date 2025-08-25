using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations;

public class PipelinePhotoConfiguration : IEntityTypeConfiguration<PipelinePhoto>
{
    public void Configure(EntityTypeBuilder<PipelinePhoto> entity)
    {
        entity.ToTable("Photos"); // Use the Photos table name from schema
        entity.HasKey(p => p.Id);

        entity.Property(p => p.UserId)
              .HasMaxLength(450)
              .IsRequired();

        entity.Property(p => p.ContentHash)
              .HasMaxLength(64)
              .IsRequired();

        entity.Property(p => p.ThumbBlobName)
              .HasMaxLength(500)
              .IsRequired();

        entity.Property(p => p.DisplayBlobName)
              .HasMaxLength(500)
              .IsRequired();

        entity.Property(p => p.Latitude)
              .HasPrecision(10, 8);

        entity.Property(p => p.Longitude)
              .HasPrecision(11, 8);

        entity.Property(p => p.Status)
              .HasMaxLength(50)
              .IsRequired()
              .HasDefaultValue("processing");

        entity.Property(p => p.CreatedAtUtc)
              .HasDefaultValueSql("GETUTCDATE()");

        entity.Property(p => p.UpdatedAtUtc)
              .HasDefaultValueSql("GETUTCDATE()");

        // Indexes for common queries
        entity.HasIndex(p => p.UserId);
        entity.HasIndex(p => p.CameraId);
        entity.HasIndex(p => p.ContentHash);
        entity.HasIndex(p => p.Status);
        entity.HasIndex(p => p.CreatedAtUtc);

        // Unique constraint to prevent duplicate uploads
        entity.HasIndex(p => new { p.UserId, p.CameraId, p.ContentHash })
              .IsUnique();
    }
}