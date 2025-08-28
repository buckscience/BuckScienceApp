using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations;

public class ApplicationUserConfiguration : IEntityTypeConfiguration<ApplicationUser>
{
    public void Configure(EntityTypeBuilder<ApplicationUser> builder)
    {
        builder.ToTable("ApplicationUsers");

        builder.HasKey(u => u.Id);

        builder.Property(u => u.AzureEntraB2CId)
            .IsRequired()
            .HasMaxLength(200);

        builder.HasIndex(u => u.AzureEntraB2CId)
            .IsUnique();

        builder.Property(u => u.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(u => u.DisplayName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(u => u.FirstName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(u => u.LastName)
            .IsRequired()
            .HasMaxLength(100);

        // Seed the initial admin user
        builder.HasData(new ApplicationUser
        {
            Id = 1,
            AzureEntraB2CId = "b300176c-0f43-4a4d-afd3-d128f8e635a1",
            FirstName = "Darrin",
            LastName = "Brandon",
            DisplayName = "Darrin B",
            Email = "darrin@buckscience.com",
            CreatedDate = new DateTime(2025, 1, 20, 0, 0, 0, DateTimeKind.Utc),
            TrialStartDate = null
        });
    }
}