using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class MigrationSmokeTests
{
    private const string InitialCreateMigration = "20261015121025_InitialCreate";

    [TestMethod]
    public void InitialCreateCanApplyRollbackAndReapply()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "LocalErp", "Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(testDirectory, "migration-smoke.db");
        Directory.CreateDirectory(testDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<ErpDbContext>()
                .UseSqlite(SqliteConnectionStringFactory.Create(databasePath))
                .Options;

            using (var context = new ErpDbContext(options))
            {
                context.Database.Migrate();
                CollectionAssert.Contains(context.Database.GetAppliedMigrations().ToList(), InitialCreateMigration);
                AssertCompanyTableExists(context, expected: true);

                var migrator = context.Database.GetService<IMigrator>();
                migrator.Migrate(Migration.InitialDatabase);
                AssertCompanyTableExists(context, expected: false);

                migrator.Migrate();
                CollectionAssert.Contains(context.Database.GetAppliedMigrations().ToList(), InitialCreateMigration);
                AssertCompanyTableExists(context, expected: true);
            }
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    private static void AssertCompanyTableExists(ErpDbContext context, bool expected)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = 'Companies';";
            var count = Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual(expected ? 1 : 0, count);
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }
}
