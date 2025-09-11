using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace BuckScience.Infrastructure.Persistence;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
{
    public AppDbContext CreateDbContext(string[] args)
    {
        // For migrations scaffolding - use Azure SQL Database
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseSqlServer(
                "Server=tcp:buckscience.database.windows.net,1433;Initial Catalog=BuckScienceDb-Dev;Persist Security Info=False;User ID=bsadmin;Password=tX0HDsWGutUnp;MultipleActiveResultSets=False;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;",
                sql => sql.UseNetTopologySuite())
            .Options;

        return new AppDbContext(options);
    }
}