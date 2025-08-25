using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<ApplicationUser> ApplicationUsers { get; }
    DbSet<Camera> Cameras { get; }
    DbSet<Photo> Photos { get; }
    DbSet<PhotoTag> PhotoTags { get; }
    DbSet<Property> Properties { get; }
    DbSet<Tag> Tags { get; }
    DbSet<Profile> Profiles { get; }
    DbSet<Weather> Weathers { get; }
    
    // Pipeline entities
    DbSet<PipelinePhoto> PipelinePhotos { get; }
    DbSet<WeatherCache> WeatherCaches { get; }
    
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}