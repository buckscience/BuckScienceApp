using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations;

public class PropertyFeatureConfiguration : IEntityTypeConfiguration<PropertyFeature>
{
    public void Configure(EntityTypeBuilder<PropertyFeature> entity)
    {
        entity.HasKey(pf => pf.Id);

        entity.Property(pf => pf.PropertyId)
              .IsRequired();

        entity.Property(pf => pf.ClassificationType)
              .HasConversion<int>()
              .IsRequired();

        // Spatial geometry column (SQL Server geometry)
        entity.Property(pf => pf.Geometry)
              .HasColumnType("geometry")
              .IsRequired();

        entity.Property(pf => pf.Notes)
              .HasMaxLength(1000);

        entity.Property(pf => pf.Weight)
              .HasColumnType("real");

        entity.Property(pf => pf.CreatedBy);

        entity.Property(pf => pf.CreatedAt)
              .HasDefaultValueSql("GETUTCDATE()")
              .IsRequired();

        // Foreign key relationship
        entity.HasOne(pf => pf.Property)
              .WithMany(p => p.PropertyFeatures)
              .HasForeignKey(pf => pf.PropertyId)
              .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        entity.HasIndex(pf => pf.PropertyId);
        entity.HasIndex(pf => pf.ClassificationType);
        entity.HasIndex(pf => pf.CreatedBy);
    }
}