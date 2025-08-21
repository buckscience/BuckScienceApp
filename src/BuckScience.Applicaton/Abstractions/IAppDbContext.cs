using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace BuckScience.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Property> Properties { get; }
    DbSet<ApplicationUser> ApplicationUsers { get; }
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}