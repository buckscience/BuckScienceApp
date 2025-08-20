using BuckScience.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;

namespace BuckScience.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<Property> Properties { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}