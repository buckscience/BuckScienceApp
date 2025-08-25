using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations;

public class WeatherCacheConfiguration : IEntityTypeConfiguration<WeatherCache>
{
    public void Configure(EntityTypeBuilder<WeatherCache> entity)
    {
        entity.ToTable("WeatherCache");
        entity.HasKey(w => w.Id);

        entity.Property(w => w.WeatherJson)
              .HasMaxLength(4000)
              .IsRequired();

        entity.Property(w => w.CreatedAtUtc)
              .HasDefaultValueSql("GETUTCDATE()");

        // Unique constraint: one weather entry per camera per day
        entity.HasIndex(w => new { w.CameraId, w.LocalDate })
              .IsUnique();

        entity.HasIndex(w => w.CameraId);
        entity.HasIndex(w => w.LocalDate);
    }
}