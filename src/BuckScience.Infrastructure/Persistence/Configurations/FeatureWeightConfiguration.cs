using BuckScience.Domain.Entities;
using BuckScience.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations;

public class FeatureWeightConfiguration : IEntityTypeConfiguration<FeatureWeight>
{
    public void Configure(EntityTypeBuilder<FeatureWeight> entity)
    {
        entity.HasKey(fw => fw.Id);

        entity.Property(fw => fw.PropertyId)
              .IsRequired();

        entity.Property(fw => fw.ClassificationType)
              .HasConversion<int>()
              .IsRequired();

        entity.Property(fw => fw.DefaultWeight)
              .HasColumnType("real")
              .IsRequired();

        entity.Property(fw => fw.UserWeight)
              .HasColumnType("real");

        entity.Property(fw => fw.SeasonalWeightsJson)
              .HasMaxLength(1000);

        entity.Property(fw => fw.IsCustom)
              .IsRequired()
              .HasDefaultValue(false);

        entity.Property(fw => fw.UpdatedAt)
              .HasDefaultValueSql("GETUTCDATE()")
              .IsRequired();

        // Foreign key relationship
        entity.HasOne(fw => fw.Property)
              .WithMany(p => p.FeatureWeights)
              .HasForeignKey(fw => fw.PropertyId)
              .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        entity.HasIndex(fw => fw.PropertyId);
        entity.HasIndex(fw => fw.ClassificationType);
        entity.HasIndex(fw => new { fw.PropertyId, fw.ClassificationType })
              .IsUnique()
              .HasDatabaseName("IX_FeatureWeights_Property_Classification");
    }
}