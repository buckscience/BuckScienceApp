using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations;

public class PropertyConfiguration : IEntityTypeConfiguration<Property>
{
    public void Configure(EntityTypeBuilder<Property> b)
    {
        b.ToTable("Properties");

        b.HasKey(p => p.Id);

        b.Property(p => p.Name)
         .HasMaxLength(200)
         .IsRequired();

        b.Property(p => p.TimeZone)
         .HasMaxLength(100)
         .IsRequired();

        b.Property(p => p.CreatedDate)
         .HasDefaultValueSql("GETUTCDATE()");

        // Spatial columns as SQL Server 'geometry'
        b.Property(p => p.Center)
         .HasColumnType("geometry")
         .IsRequired();

        b.Property(p => p.Boundary)
         .HasColumnType("geometry")
         .IsRequired(false);

        // Optional: enforce SRID 4326 via a check constraint
        b.ToTable(t =>
        {
            t.HasCheckConstraint("CK_Properties_SRID",
                "([Center].STSrid = 4326) AND ([Boundary] IS NULL OR [Boundary].STSrid = 4326)");
        });
    }
}