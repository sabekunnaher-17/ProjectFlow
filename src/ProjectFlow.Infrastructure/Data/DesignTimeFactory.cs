using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProjectFlow.Infrastructure.Data;

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Migration generation does not open a connection. Applying migrations
        // requires an explicitly supplied target; this design-only name is not runtime config.
        var connection = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")
            ?? "Server=(localdb)\\MSSQLLocalDB;Database=ProjectFlow_DesignOnly;Integrated Security=True;TrustServerCertificate=True";
        return new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
    }
}
