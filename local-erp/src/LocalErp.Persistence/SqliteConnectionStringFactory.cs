using Microsoft.Data.Sqlite;

namespace LocalErp.Persistence;

public static class SqliteConnectionStringFactory
{
    public static string Create(string databasePath)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(databasePath);

        var fullPath = Path.GetFullPath(databasePath);
        var directory = Path.GetDirectoryName(fullPath)
            ?? throw new InvalidOperationException("Database path must have a parent directory.");

        Directory.CreateDirectory(directory);

        var builder = new SqliteConnectionStringBuilder
        {
            DataSource = fullPath,
            Mode = SqliteOpenMode.ReadWriteCreate,
            ForeignKeys = true,
            DefaultTimeout = 15
        };

        return builder.ToString();
    }
}
