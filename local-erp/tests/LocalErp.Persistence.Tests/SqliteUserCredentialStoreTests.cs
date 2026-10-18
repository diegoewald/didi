using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using LocalErp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class SqliteUserCredentialStoreTests
{
    private static readonly DateTime AuditTime = new(2026, 10, 18, 0, 30, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task TryChangePasswordAsyncPersistsHashResetAndAuditTogether()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var store = new SqliteUserCredentialStore(database);
        var user = new User("diego", "old-hash");
        user.RecordFailedSignIn(AuditTime.AddMinutes(-1), TimeSpan.FromMinutes(15));
        await users.AddAsync(user);
        var auditEvent = CreateAuditEvent(user.Id);

        var changed = await store.TryChangePasswordAsync(
            user.Id,
            "old-hash",
            "new-hash",
            auditEvent);

        Assert.IsTrue(changed);
        var reloaded = await users.GetByIdAsync(user.Id)
            ?? throw new InvalidOperationException("Test user disappeared after password change.");
        Assert.AreEqual("new-hash", reloaded.PasswordHash);
        Assert.AreEqual(0, reloaded.FailedSignInCount);
        Assert.IsNull(reloaded.SignInBlockedUntilUtc);

        await using var context = database.CreateDbContext();
        var persistedAudit = await context.SecurityAuditEvents.SingleAsync();
        Assert.AreEqual(SecurityAuditCodes.AccountPasswordChanged, persistedAudit.EventCode);
        Assert.AreEqual(user.Id, persistedAudit.UserId);
        Assert.AreEqual(user.Id, persistedAudit.ActorUserId);
    }

    [TestMethod]
    public async Task TryChangePasswordAsyncRejectsStaleExpectedHashWithoutAudit()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var store = new SqliteUserCredentialStore(database);
        var user = new User("diego", "current-hash");
        await users.AddAsync(user);

        var changed = await store.TryChangePasswordAsync(
            user.Id,
            "stale-hash",
            "new-hash",
            CreateAuditEvent(user.Id));

        Assert.IsFalse(changed);
        var reloaded = await users.GetByIdAsync(user.Id)
            ?? throw new InvalidOperationException("Test user disappeared after rejected password change.");
        Assert.AreEqual("current-hash", reloaded.PasswordHash);
        await using var context = database.CreateDbContext();
        Assert.AreEqual(0, await context.SecurityAuditEvents.CountAsync());
    }

    [TestMethod]
    public async Task TryChangePasswordAsyncRejectsInactiveUserWithoutAudit()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var store = new SqliteUserCredentialStore(database);
        var user = new User("diego", "old-hash");
        user.Deactivate();
        await users.AddAsync(user);

        var changed = await store.TryChangePasswordAsync(
            user.Id,
            "old-hash",
            "new-hash",
            CreateAuditEvent(user.Id));

        Assert.IsFalse(changed);
        var reloaded = await users.GetByIdAsync(user.Id)
            ?? throw new InvalidOperationException("Test user disappeared after rejected password change.");
        Assert.AreEqual("old-hash", reloaded.PasswordHash);
        await using var context = database.CreateDbContext();
        Assert.AreEqual(0, await context.SecurityAuditEvents.CountAsync());
    }

    [TestMethod]
    public async Task TryChangePasswordAsyncRollsBackHashWhenAuditInsertFails()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var auditStore = new EfSecurityAuditStore(database);
        var store = new SqliteUserCredentialStore(database);
        var user = new User("diego", "old-hash");
        await users.AddAsync(user);
        var duplicateAuditEvent = CreateAuditEvent(user.Id);
        await auditStore.AppendAsync(duplicateAuditEvent);

        await Assert.ThrowsExactlyAsync<DbUpdateException>(
            () => store.TryChangePasswordAsync(
                user.Id,
                "old-hash",
                "new-hash",
                duplicateAuditEvent));

        var reloaded = await users.GetByIdAsync(user.Id)
            ?? throw new InvalidOperationException("Test user disappeared after rollback.");
        Assert.AreEqual("old-hash", reloaded.PasswordHash);
        await using var context = database.CreateDbContext();
        Assert.AreEqual(1, await context.SecurityAuditEvents.CountAsync());
    }

    [TestMethod]
    public async Task ConcurrentPasswordChangesWithSameExpectedHashCommitExactlyOne()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var store = new SqliteUserCredentialStore(database);
        var user = new User("diego", "old-hash");
        await users.AddAsync(user);

        var results = await Task.WhenAll(
            store.TryChangePasswordAsync(
                user.Id,
                "old-hash",
                "new-hash-one",
                CreateAuditEvent(user.Id)),
            store.TryChangePasswordAsync(
                user.Id,
                "old-hash",
                "new-hash-two",
                CreateAuditEvent(user.Id)));

        Assert.AreEqual(1, results.Count(result => result));
        Assert.AreEqual(1, results.Count(result => !result));

        var reloaded = await users.GetByIdAsync(user.Id)
            ?? throw new InvalidOperationException("Test user disappeared after concurrent password changes.");
        Assert.IsTrue(reloaded.PasswordHash is "new-hash-one" or "new-hash-two");

        await using var context = database.CreateDbContext();
        Assert.AreEqual(1, await context.SecurityAuditEvents.CountAsync());
    }

    private static SecurityAuditEvent CreateAuditEvent(Guid userId)
    {
        return new SecurityAuditEvent(
            AuditTime,
            SecurityAuditCodes.AccountPasswordChanged,
            userId: userId,
            actorUserId: userId);
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
            var databasePath = Path.Combine(directory, "user-credential.db");
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
