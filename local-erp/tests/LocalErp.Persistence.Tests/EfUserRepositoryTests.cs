using LocalErp.Domain.Users;
using LocalErp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class EfUserRepositoryTests
{
    [TestMethod]
    public async Task RepositoryPersistsFindsAndFiltersUsers()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new EfUserRepository(database);

        var active = new User("Diego", "hash-active");
        var inactive = new User("Inactive", "hash-inactive");
        inactive.Deactivate();

        await repository.AddAsync(active);
        await repository.AddAsync(inactive);

        var loaded = await repository.GetByNormalizedUsernameAsync(User.NormalizeUsername(" diego "))
            ?? throw new AssertFailedException("Persisted user was not found by normalized username.");
        Assert.AreEqual(active.Id, loaded.Id);
        Assert.AreEqual("hash-active", loaded.PasswordHash);

        var activeOnly = await repository.ListAsync(includeInactive: false);
        Assert.AreEqual(1, activeOnly.Count);
        Assert.AreEqual(active.Id, activeOnly[0].Id);

        var all = await repository.ListAsync(includeInactive: true);
        Assert.AreEqual(2, all.Count);
    }

    [TestMethod]
    public async Task DatabaseRejectsDuplicateNormalizedUsername()
    {
        await using var database = await TestDatabase.CreateAsync();
        var repository = new EfUserRepository(database);

        await repository.AddAsync(new User("Diego", "hash-one"));

        await Assert.ThrowsExactlyAsync<DbUpdateException>(
            () => repository.AddAsync(new User("  DIEGO  ", "hash-two")));
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
            var directory = Path.Combine(Path.GetTempPath(), "LocalErp", "Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var databasePath = Path.Combine(directory, "users.db");
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
