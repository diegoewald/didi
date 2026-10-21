using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Units;
using LocalErp.Domain.Users;
using LocalErp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class EfPurchaseRequestRepositoryTests
{
    private static readonly DateTime CreatedAt = new(2026, 10, 21, 12, 30, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task RepositoryPersistsFiltersAndSynchronizesLines()
    {
        var testDatabase = await TestDatabase.CreateAsync();
        await using var cleanup = testDatabase;
        var userRepository = new EfUserRepository(testDatabase);
        var unitRepository = new EfMeasurementUnitRepository(testDatabase);
        var requestRepository = new EfPurchaseRequestRepository(testDatabase);

        var user = new User("buyer", "hash");
        var unit = new MeasurementUnit("Peça");
        await userRepository.AddAsync(user);
        await unitRepository.AddAsync(unit);

        var request = new PurchaseRequest(user.Id, CreatedAt, "Primeira necessidade");
        var firstLine = request.AddLine("Rolamento", 2m, unit.Id);
        var removedLine = request.AddLine("Parafuso", 10m, unit.Id);
        await requestRepository.AddAsync(request);

        var loaded = await requestRepository.GetByIdAsync(request.Id)
            ?? throw new AssertFailedException("Persisted purchase request was not found.");
        loaded.UpdateLine(firstLine.Id, "Rolamento 6204", 3m, unit.Id, new DateOnly(2026, 11, 5));
        loaded.RemoveLine(removedLine.Id);
        var addedLine = loaded.AddLine("Arruela", 8m, unit.Id);
        await requestRepository.UpdateAsync(loaded);

        var updated = await requestRepository.GetByIdAsync(request.Id)
            ?? throw new AssertFailedException("Updated purchase request was not found.");
        Assert.AreEqual(2, updated.Lines.Count);
        var persistedFirst = updated.Lines.Single(line => line.Id == firstLine.Id);
        Assert.AreEqual("Rolamento 6204", persistedFirst.Description);
        Assert.AreEqual(3m, persistedFirst.Quantity);
        Assert.AreEqual(new DateOnly(2026, 11, 5), persistedFirst.NeededByDate);
        Assert.IsFalse(updated.Lines.Any(line => line.Id == removedLine.Id));
        Assert.IsTrue(updated.Lines.Any(line => line.Id == addedLine.Id));

        updated.Submit(user.Id, CreatedAt.AddHours(1));
        await requestRepository.UpdateAsync(updated);

        var drafts = await requestRepository.ListAsync(PurchaseRequestStatus.Draft);
        var submitted = await requestRepository.ListAsync(PurchaseRequestStatus.Submitted);
        Assert.AreEqual(0, drafts.Count);
        Assert.AreEqual(1, submitted.Count);
        Assert.AreEqual(request.Id, submitted[0].Id);
    }

    [TestMethod]
    public async Task RepositoryEnforcesUserAndMeasurementUnitForeignKeys()
    {
        var testDatabase = await TestDatabase.CreateAsync();
        await using var cleanup = testDatabase;
        var userRepository = new EfUserRepository(testDatabase);
        var requestRepository = new EfPurchaseRequestRepository(testDatabase);

        var orphanRequest = new PurchaseRequest(Guid.NewGuid(), CreatedAt);
        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => requestRepository.AddAsync(orphanRequest));

        var user = new User("buyer", "hash");
        await userRepository.AddAsync(user);
        var invalidUnitRequest = new PurchaseRequest(user.Id, CreatedAt);
        invalidUnitRequest.AddLine("Item", 1m, Guid.NewGuid());

        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => requestRepository.AddAsync(invalidUnitRequest));
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
            var databasePath = Path.Combine(directory, "purchase-requests.db");
            var connectionString = $"{SqliteConnectionStringFactory.Create(databasePath)};Pooling=False";
            var options = new DbContextOptionsBuilder<ErpDbContext>()
                .UseSqlite(connectionString)
                .Options;

            var database = new TestDatabase(directory, options);
            await using var context = database.CreateDbContext();
            await context.Database.MigrateAsync();
            return database;
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
