using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations
{
    public class WeatherConfiguration : IEntityTypeConfiguration<Weather>
    {
        public void Configure(EntityTypeBuilder<Weather> entity)
        {
            entity.HasKey(w => w.Id);

            // Location and time fields for efficient lookups
            entity.Property(w => w.Latitude).IsRequired();
            entity.Property(w => w.Longitude).IsRequired();
            entity.Property(w => w.Date).IsRequired();
            entity.Property(w => w.Hour).IsRequired();
            entity.Property(w => w.DateTime).IsRequired();
            entity.Property(w => w.DateTimeEpoch).IsRequired();

            // Composite index for weather lookup by location and date/hour
            entity.HasIndex(w => new { w.Latitude, w.Longitude, w.Date })
                  .HasDatabaseName("IX_Weather_Location_Date");
            
            entity.HasIndex(w => new { w.Latitude, w.Longitude, w.Date, w.Hour })
                  .HasDatabaseName("IX_Weather_Location_Date_Hour");

            // Helpful indexes for lookups by time
            entity.HasIndex(w => w.DateTime);
            entity.HasIndex(w => w.DateTimeEpoch);

            // String lengths
            entity.Property(w => w.WindDirectionText).HasMaxLength(16);
            entity.Property(w => w.PressureTrend).HasMaxLength(16);
            entity.Property(w => w.Conditions).HasMaxLength(256);
            entity.Property(w => w.Icon).HasMaxLength(128);
            entity.Property(w => w.MoonPhaseText).HasMaxLength(64);

            // Relationship is configured on Photo side:
            // Photo.WeatherId (nullable) -> Weather.Id with OnDelete(SetNull) in PhotoConfiguration.
        }
    }
}