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

        entity.Property(fw => fw.ApplicationUserId)
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

        entity.Property(fw => fw.UpdatedAt)
              .HasDefaultValueSql("GETUTCDATE()")
              .IsRequired();

        // Foreign key relationship
        entity.HasOne(fw => fw.ApplicationUser)
              .WithMany(u => u.FeatureWeights)
              .HasForeignKey(fw => fw.ApplicationUserId)
              .OnDelete(DeleteBehavior.Cascade);

        // Indexes
        entity.HasIndex(fw => fw.ApplicationUserId);
        entity.HasIndex(fw => fw.ClassificationType);
        entity.HasIndex(fw => new { fw.ApplicationUserId, fw.ClassificationType })
              .IsUnique()
              .HasDatabaseName("IX_FeatureWeights_User_Classification");
    }
}