using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class SqliteDatabaseRuntimeConfiguratorTests
{
    [TestMethod]
    public async Task ConfigureAsyncEnablesWalAndForeignKeysForFileDatabase()
    {
        var root = Path.Combine(Path.GetTempPath(), "localerp-tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(root, "localerp.db");
        var connectionString = SqliteConnectionStringFactory.Create(databasePath);
        var configurator = new SqliteDatabaseRuntimeConfigurator(connectionString);

        await configurator.ConfigureAsync();

        await using var connection = new SqliteConnection(connectionString);
        await connection.OpenAsync();

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode;";
        var journalMode = (await command.ExecuteScalarAsync())?.ToString();
        Assert.IsTrue(string.Equals("wal", journalMode, StringComparison.OrdinalIgnoreCase));

        command.CommandText = "PRAGMA foreign_keys;";
        var foreignKeys = Convert.ToInt32(
            await command.ExecuteScalarAsync(),
            System.Globalization.CultureInfo.InvariantCulture);
        Assert.AreEqual(1, foreignKeys);
    }

    [TestMethod]
    public async Task ConfigureAsyncIsIdempotent()
    {
        var root = Path.Combine(Path.GetTempPath(), "localerp-tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(root, "localerp.db");
        var connectionString = SqliteConnectionStringFactory.Create(databasePath);
        var configurator = new SqliteDatabaseRuntimeConfigurator(connectionString);

        await configurator.ConfigureAsync();
        await configurator.ConfigureAsync();
    }
}
