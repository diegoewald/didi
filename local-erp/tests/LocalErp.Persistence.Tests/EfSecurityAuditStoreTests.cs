using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using LocalErp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class EfSecurityAuditStoreTests
{
    private static readonly DateTime UtcNow = new(2026, 10, 16, 15, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task AppendAsyncPersistsAuthenticationEventWithoutCredentials()
    {
        await using var database = await TestDatabase.CreateAsync();
        var user = new User("diego", "password-hash-that-must-not-appear-in-audit");
        await database.InsertUserAsync(user);
        var store = new EfSecurityAuditStore(database);

        await store.AppendAsync(
            new SecurityAuditEvent(
                UtcNow,
                SecurityAuditCodes.AuthenticationSignInBlocked,
                user.Id,
                SecurityAuditDetailCodes.TemporarilyBlocked));

        await using var verification = database.CreateDbContext();
        var persisted = await verification.SecurityAuditEvents.SingleAsync();
        Assert.AreEqual(user.Id, persisted.UserId);
        Assert.AreEqual(UtcNow, persisted.OccurredAtUtc);
        Assert.AreEqual(SecurityAuditCodes.AuthenticationSignInBlocked, persisted.EventCode);
        Assert.AreEqual(SecurityAuditDetailCodes.TemporarilyBlocked, persisted.DetailCode);
        Assert.IsFalse(persisted.EventCode.Contains("password", StringComparison.OrdinalIgnoreCase));
        Assert.IsFalse((persisted.DetailCode ?? string.Empty).Contains("password-hash", StringComparison.OrdinalIgnoreCase));
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

            var databasePath = Path.Combine(directory, "security-audit.db");
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
