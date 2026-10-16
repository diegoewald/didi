using Microsoft.Data.Sqlite;

namespace LocalErp.Persistence;

public sealed class SqliteDatabaseRuntimeConfigurator(string connectionString)
{
    private readonly string _connectionString = string.IsNullOrWhiteSpace(connectionString)
        ? throw new ArgumentException("Connection string cannot be blank.", nameof(connectionString))
        : connectionString;

    public async Task ConfigureAsync(CancellationToken cancellationToken = default)
    {
        await using var connection = new SqliteConnection(_connectionString);
        await connection.OpenAsync(cancellationToken);

        using var command = connection.CreateCommand();
        command.CommandText = "PRAGMA journal_mode = WAL;";

        var journalMode = (await command.ExecuteScalarAsync(cancellationToken))?.ToString();
        if (!string.Equals(journalMode, "wal", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                $"SQLite WAL mode could not be enabled. Current journal mode: '{journalMode ?? "unknown"}'.");
        }

        command.CommandText = "PRAGMA foreign_keys;";
        var foreignKeys = await command.ExecuteScalarAsync(cancellationToken);
        if (Convert.ToInt32(foreignKeys, System.Globalization.CultureInfo.InvariantCulture) != 1)
        {
            throw new InvalidOperationException("SQLite foreign key enforcement is disabled.");
        }
    }
}
