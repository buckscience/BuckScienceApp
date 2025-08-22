using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace BuckScience.Infrastructure.Persistence.Configurations
{
    public class PhotoTagConfiguration : IEntityTypeConfiguration<PhotoTag>
    {
        public void Configure(EntityTypeBuilder<PhotoTag> entity)
        {
            // Composite PK
            entity.HasKey(pt => new { pt.PhotoId, pt.TagId });

            // Indexes (optional but handy)
            entity.HasIndex(pt => pt.PhotoId);
            entity.HasIndex(pt => pt.TagId);

            // Relationships
            entity.HasOne(pt => pt.Photo)
                  .WithMany(p => p.PhotoTags)
                  .HasForeignKey(pt => pt.PhotoId)
                  .OnDelete(DeleteBehavior.Cascade);

            entity.HasOne(pt => pt.Tag)
                  .WithMany(t => t.PhotoTags)
                  .HasForeignKey(pt => pt.TagId)
                  .OnDelete(DeleteBehavior.Cascade);
        }
    }
}