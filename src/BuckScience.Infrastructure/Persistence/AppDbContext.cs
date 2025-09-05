using BuckScience.Application.Abstractions;
using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Reflection;

namespace BuckScience.Infrastructure.Persistence;

// Concrete EF Core DbContext (implementation)
public class AppDbContext : DbContext, IAppDbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    public DbSet<ApplicationUser> ApplicationUsers => Set<ApplicationUser>();
    public DbSet<Property> Properties => Set<Property>();
    public DbSet<PropertyFeature> PropertyFeatures => Set<PropertyFeature>();

    public DbSet<Camera> Cameras => Set<Camera>();
    public DbSet<CameraPlacementHistory> CameraPlacementHistories => Set<CameraPlacementHistory>();
    public DbSet<Photo> Photos => Set<Photo>();
    public DbSet<PhotoTag> PhotoTags => Set<PhotoTag>();
    public DbSet<PropertyTag> PropertyTags => Set<PropertyTag>();
    public DbSet<Tag> Tags => Set<Tag>();
    public DbSet<Profile> Profiles => Set<Profile>();
    public DbSet<Weather> Weathers => Set<Weather>();


    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Ensure all IEntityTypeConfiguration<T> in this assembly are applied,
        // including PropertyConfiguration that maps spatial columns to 'geometry'.
        modelBuilder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

    }
}