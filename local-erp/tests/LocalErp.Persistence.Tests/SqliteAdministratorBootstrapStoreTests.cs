using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using LocalErp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class SqliteAdministratorBootstrapStoreTests
{
    [TestMethod]
    public async Task TryBootstrapAsyncPersistsCompleteAdministratorGraph()
    {
        await using var database = await TestDatabase.CreateAsync();
        var store = new SqliteAdministratorBootstrapStore(database);
        var user = new User("admin", "hash");
        var role = new Role("Administrator");

        Assert.IsTrue(await store.IsBootstrapRequiredAsync());

        var committed = await store.TryBootstrapAsync(
            user,
            role,
            PermissionCatalog.All.ToArray());

        Assert.IsTrue(committed);
        Assert.IsFalse(await store.IsBootstrapRequiredAsync());

        await using var context = database.CreateDbContext();
        Assert.AreEqual(1, await context.Users.CountAsync());
        Assert.AreEqual(1, await context.Roles.CountAsync());
        Assert.AreEqual(1, await context.UserRoles.CountAsync());
        Assert.AreEqual(PermissionCatalog.All.Count, await context.RolePermissions.CountAsync());

        var assignment = await context.UserRoles.SingleAsync();
        Assert.AreEqual(user.Id, assignment.UserId);
        Assert.AreEqual(role.Id, assignment.RoleId);
    }

    [TestMethod]
    public async Task TryBootstrapAsyncReturnsFalseWithoutWritingWhenUserAlreadyExists()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using (var context = database.CreateDbContext())
        {
            context.Users.Add(new User("existing", "hash"));
            await context.SaveChangesAsync();
        }

        var store = new SqliteAdministratorBootstrapStore(database);
        var committed = await store.TryBootstrapAsync(
            new User("admin", "hash"),
            new Role("Administrator"),
            PermissionCatalog.All.ToArray());

        Assert.IsFalse(committed);

        await using var verification = database.CreateDbContext();
        Assert.AreEqual(1, await verification.Users.CountAsync());
        Assert.AreEqual(0, await verification.Roles.CountAsync());
        Assert.AreEqual(0, await verification.UserRoles.CountAsync());
        Assert.AreEqual(0, await verification.RolePermissions.CountAsync());
    }

    [TestMethod]
    public async Task TryBootstrapAsyncRollsBackEveryChangeWhenPersistenceFails()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using (var context = database.CreateDbContext())
        {
            context.Roles.Add(new Role("Administrator"));
            await context.SaveChangesAsync();
        }

        var store = new SqliteAdministratorBootstrapStore(database);

        await Assert.ThrowsExactlyAsync<DbUpdateException>(
            () => store.TryBootstrapAsync(
                new User("admin", "hash"),
                new Role("Administrator"),
                PermissionCatalog.All.ToArray()));

        await using var verification = database.CreateDbContext();
        Assert.AreEqual(0, await verification.Users.CountAsync());
        Assert.AreEqual(1, await verification.Roles.CountAsync());
        Assert.AreEqual(0, await verification.UserRoles.CountAsync());
        Assert.AreEqual(0, await verification.RolePermissions.CountAsync());
    }

    [TestMethod]
    public async Task ConcurrentBootstrapAttemptsCommitExactlyOneAdministrator()
    {
        await using var database = await TestDatabase.CreateAsync();
        var firstStore = new SqliteAdministratorBootstrapStore(database);
        var secondStore = new SqliteAdministratorBootstrapStore(database);

        var firstAttempt = Task.Run(() => firstStore.TryBootstrapAsync(
            new User("admin-one", "hash-one"),
            new Role("Administrator"),
            PermissionCatalog.All.ToArray()));

        var secondAttempt = Task.Run(() => secondStore.TryBootstrapAsync(
            new User("admin-two", "hash-two"),
            new Role("Administrator"),
            PermissionCatalog.All.ToArray()));

        var results = await Task.WhenAll(firstAttempt, secondAttempt);

        Assert.AreEqual(1, results.Count(result => result));
        Assert.AreEqual(1, results.Count(result => !result));

        await using var verification = database.CreateDbContext();
        Assert.AreEqual(1, await verification.Users.CountAsync());
        Assert.AreEqual(1, await verification.Roles.CountAsync());
        Assert.AreEqual(1, await verification.UserRoles.CountAsync());
        Assert.AreEqual(PermissionCatalog.All.Count, await verification.RolePermissions.CountAsync());
    }

    private sealed class TestDatabase : IDbContextFactory<ErpDbContext>, IAsyncDisposable
    {
        private readonly string _directory;
        private readonly DbContextOptions<ErpDbContext> _options;

        private TestDatabase(string directory, DbContextOptions<ErpDbContext> options)
        {
            _directory = directory;
            _options = options;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var directory = Path.Combine(
                Path.GetTempPath(),
                "LocalErp",
                "Tests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);

            var databasePath = Path.Combine(directory, "administrator-bootstrap.db");
            var connectionString = $"{SqliteConnectionStringFactory.Create(databasePath)};Pooling=False";
            var options = new DbContextOptionsBuilder<ErpDbContext>()
                .UseSqlite(connectionString)
                .Options;

            var database = new TestDatabase(directory, options);
            await using var context = database.CreateDbContext();
            await context.Database.MigrateAsync();
            return database;
        }

        public ErpDbContext CreateDbContext()
        {
            return new ErpDbContext(_options);
        }

        public Task<ErpDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateDbContext());
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
