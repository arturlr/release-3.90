using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Nop.Data;

/// <summary>
/// Design-time factory for NopDbContext. Used by EF Core tooling (dotnet ef migrations).
/// The connection string is only used for migration generation — not at runtime.
/// </summary>
public class NopDbContextFactory : IDesignTimeDbContextFactory<NopDbContext>
{
    public NopDbContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<NopDbContext>();
        optionsBuilder.UseSqlServer("Server=.;Database=NopCommerce_Design;Trusted_Connection=True;TrustServerCertificate=True");

        return new NopDbContext(optionsBuilder.Options);
    }
}
