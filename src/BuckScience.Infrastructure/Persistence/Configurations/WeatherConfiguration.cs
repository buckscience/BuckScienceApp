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

            entity.Property(w => w.DateTime).IsRequired();
            entity.Property(w => w.DateTimeEpoch).IsRequired();

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