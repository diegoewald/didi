using LocalErp.Application.Security;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using LocalErp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class SqliteAuthenticationAttemptStoreTests
{
    private static readonly DateTime UtcNow = new(2026, 10, 16, 13, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task RecordFailedAttemptAsyncPersistsProgressiveThrottle()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = new User("diego", "hash");
        await database.InsertUserAsync(user);
        var store = new SqliteAuthenticationAttemptStore(database);

        await store.RecordFailedAttemptAsync(user.Id, UtcNow);
        await store.RecordFailedAttemptAsync(user.Id, UtcNow);
        await store.RecordFailedAttemptAsync(user.Id, UtcNow);

        await using var verification = database.CreateDbContext();
        var persisted = await verification.Users.SingleAsync();
        Assert.AreEqual(3, persisted.FailedSignInCount);
        Assert.AreEqual(UtcNow.AddSeconds(2), persisted.SignInBlockedUntilUtc);
        var auditEvents = await verification.SecurityAuditEvents.OrderBy(item => item.EventCode).ToListAsync();
        Assert.AreEqual(4, auditEvents.Count);
        Assert.AreEqual(3, auditEvents.Count(item => item.EventCode == SecurityAuditCodes.AuthenticationSignInFailed));
        var throttle = auditEvents.Single(item => item.EventCode == SecurityAuditCodes.AuthenticationThrottleApplied);
        Assert.AreEqual(SecurityAuditDetailCodes.ProgressiveDelay, throttle.DetailCode);
    }

    [TestMethod]
    public async Task RecordFailedAttemptAsyncAdvancesAfterThrottleExpires()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = new User("diego", "hash");
        await database.InsertUserAsync(user);
        var store = new SqliteAuthenticationAttemptStore(database);

        await store.RecordFailedAttemptAsync(user.Id, UtcNow);
        await store.RecordFailedAttemptAsync(user.Id, UtcNow);
        await store.RecordFailedAttemptAsync(user.Id, UtcNow);
        await store.RecordFailedAttemptAsync(user.Id, UtcNow.AddSeconds(3));

        await using var verification = database.CreateDbContext();
        var persisted = await verification.Users.SingleAsync();
        Assert.AreEqual(4, persisted.FailedSignInCount);
        Assert.AreEqual(UtcNow.AddSeconds(13), persisted.SignInBlockedUntilUtc);
        Assert.AreEqual(6, await verification.SecurityAuditEvents.CountAsync());
    }

    [TestMethod]
    public async Task RecordFailedAttemptAsyncAuditsFifteenMinuteLockoutAtThreshold()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = new User("diego", "hash");
        user.RecordFailedSignIn(UtcNow.AddMinutes(-1), TimeSpan.Zero);
        user.RecordFailedSignIn(UtcNow.AddMinutes(-1), TimeSpan.Zero);
        user.RecordFailedSignIn(UtcNow.AddMinutes(-1), TimeSpan.Zero);
        user.RecordFailedSignIn(UtcNow.AddMinutes(-1), TimeSpan.Zero);
        await database.InsertUserAsync(user);
        var store = new SqliteAuthenticationAttemptStore(database);

        await store.RecordFailedAttemptAsync(user.Id, UtcNow);

        await using var verification = database.CreateDbContext();
        var persisted = await verification.Users.SingleAsync();
        Assert.AreEqual(AuthenticationThrottlePolicy.LockoutThreshold, persisted.FailedSignInCount);
        Assert.AreEqual(UtcNow.AddMinutes(15), persisted.SignInBlockedUntilUtc);
        var throttle = await verification.SecurityAuditEvents
            .SingleAsync(item => item.EventCode == SecurityAuditCodes.AuthenticationThrottleApplied);
        Assert.AreEqual(SecurityAuditDetailCodes.TemporaryLockout, throttle.DetailCode);
    }

    [TestMethod]
    public async Task CompleteSuccessfulAttemptAsyncResetsFailuresAndUpgradesHash()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = new User("diego", "old-hash");
        await database.InsertUserAsync(user);
        var store = new SqliteAuthenticationAttemptStore(database);

        await store.RecordFailedAttemptAsync(user.Id, UtcNow);
        await store.RecordFailedAttemptAsync(user.Id, UtcNow);

        var completed = await store.CompleteSuccessfulAttemptAsync(
            user.Id,
            UtcNow,
            "new-hash");

        Assert.IsTrue(completed);
        await using var verification = database.CreateDbContext();
        var persisted = await verification.Users.SingleAsync();
        Assert.AreEqual("new-hash", persisted.PasswordHash);
        Assert.AreEqual(0, persisted.FailedSignInCount);
        Assert.IsNull(persisted.SignInBlockedUntilUtc);
        Assert.AreEqual(3, await verification.SecurityAuditEvents.CountAsync());
        var success = await verification.SecurityAuditEvents
            .SingleAsync(item => item.EventCode == SecurityAuditCodes.AuthenticationSignInSucceeded);
        Assert.AreEqual(user.Id, success.UserId);
    }

    [TestMethod]
    public async Task CompleteSuccessfulAttemptAsyncFailsClosedWhileBlocked()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = new User("diego", "hash");
        user.RecordFailedSignIn(UtcNow, TimeSpan.FromMinutes(15));
        await database.InsertUserAsync(user);
        var store = new SqliteAuthenticationAttemptStore(database);

        var completed = await store.CompleteSuccessfulAttemptAsync(
            user.Id,
            UtcNow,
            null);

        Assert.IsFalse(completed);
        await using var verification = database.CreateDbContext();
        var persisted = await verification.Users.SingleAsync();
        Assert.AreEqual(1, persisted.FailedSignInCount);
        Assert.AreEqual(UtcNow.AddMinutes(15), persisted.SignInBlockedUntilUtc);
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

            var databasePath = Path.Combine(directory, "authentication-attempts.db");
            var connectionString = $"{SqliteConnectionStringFactory.Create(databasePath)};Pooling=False";
            var options = new DbContextOptionsBuilder<ErpDbContext>()
                .UseSqlite(connectionString)
                .Options;

            var database = new TestDatabase(directory, options);
            await using var context = database.CreateDbContext();
            await context.Database.EnsureCreatedAsync();
            return database;
        }

        public async Task InsertUserAsync(User user)
        {
            await using var context = CreateDbContext();
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }

        public ErpDbContext CreateDbContext() => new(_options);

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
