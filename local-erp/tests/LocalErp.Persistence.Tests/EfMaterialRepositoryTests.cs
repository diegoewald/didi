using LocalErp.Domain.Materials;
using LocalErp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class EfMaterialRepositoryTests
{
    [TestMethod]
    public async Task RepositoryPersistsReadsFiltersOrdersAndUpdatesMaterial()
    {
        var testDatabase = await TestDatabase.CreateAsync();
        await using var cleanup = testDatabase;
        var repository = new EfMaterialRepository(testDatabase);

        var active = new Material("Material B");
        var inactive = new Material("Material A");
        inactive.Deactivate();

        await repository.AddAsync(active);
        await repository.AddAsync(inactive);

        var activeOnly = await repository.ListAsync(includeInactive: false);
        Assert.AreEqual(1, activeOnly.Count);
        Assert.AreEqual(active.Id, activeOnly[0].Id);

        var all = await repository.ListAsync(includeInactive: true);
        Assert.AreEqual(2, all.Count);
        Assert.AreEqual(inactive.Id, all[0].Id);
        Assert.AreEqual(active.Id, all[1].Id);

        var loaded = await repository.GetByIdAsync(active.Id)
            ?? throw new AssertFailedException("Persisted material was not found.");
        loaded.Rename("Material Renomeado");
        await repository.UpdateAsync(loaded);

        var updated = await repository.GetByIdAsync(active.Id)
            ?? throw new AssertFailedException("Updated material was not found.");
        Assert.AreEqual("Material Renomeado", updated.Name);
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
            var databasePath = Path.Combine(directory, "repository.db");
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
