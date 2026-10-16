using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace LocalErp.Persistence;

public sealed class ErpDbContextDesignTimeFactory : IDesignTimeDbContextFactory<ErpDbContext>
{
    public ErpDbContext CreateDbContext(string[] args)
    {
        var designDirectory = Path.Combine(Path.GetTempPath(), "LocalErp", "DesignTime");
        var databasePath = Path.Combine(designDirectory, "localerp-design.db");
        var connectionString = SqliteConnectionStringFactory.Create(databasePath);

        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseSqlite(connectionString)
            .Options;

        return new ErpDbContext(options);
    }
}
