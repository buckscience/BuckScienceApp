using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BuckScience.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // Only for migrations scaffolding
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(
                "Server=(localdb)\\MSSQLLocalDB;Database=BuckScienceDb;Trusted_Connection=True;MultipleActiveResultSets=true",
                sql => sql.UseNetTopologySuite())
            .Options;

        return new AppDbContext(options);
    }
}