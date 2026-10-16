using Microsoft.Data.Sqlite;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class SqliteConnectionStringFactoryTests
{
    [TestMethod]
    public void CreateEnablesForeignKeysAndUsesLocalFile()
    {
        var root = Path.Combine(Path.GetTempPath(), "localerp-tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(root, "localerp.db");

        var connectionString = SqliteConnectionStringFactory.Create(databasePath);
        var parsed = new SqliteConnectionStringBuilder(connectionString);

        Assert.AreEqual(Path.GetFullPath(databasePath), parsed.DataSource);
        Assert.IsTrue(parsed.ForeignKeys == true);
        Assert.AreEqual(SqliteOpenMode.ReadWriteCreate, parsed.Mode);
        Assert.AreNotEqual(SqliteCacheMode.Shared, parsed.Cache);
        Assert.AreEqual(15, parsed.DefaultTimeout);
    }
}
