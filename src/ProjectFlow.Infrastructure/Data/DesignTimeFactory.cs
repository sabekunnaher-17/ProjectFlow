using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace ProjectFlow.Infrastructure.Data;

public sealed class DesignTimeFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        // Offline model/script generation must be explicit and cannot accidentally
        // connect to a locally installed SQL instance. Database updates require config.
        var connection = args.Contains("--design-only")
            ? "Server=design-only.invalid;Database=ProjectFlow_DesignOnly;Integrated Security=True;Encrypt=True;Connect Timeout=1"
            : Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection");
        if (string.IsNullOrWhiteSpace(connection))
            throw new InvalidOperationException("Set ConnectionStrings__DefaultConnection for an explicitly approved database. Use -- --design-only only for offline migration/script generation.");
        return new(new DbContextOptionsBuilder<ApplicationDbContext>().UseSqlServer(connection).Options);
    }
}
