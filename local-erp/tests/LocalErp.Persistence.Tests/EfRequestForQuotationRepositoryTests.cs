using System.Globalization;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Suppliers;
using LocalErp.Domain.Units;
using LocalErp.Domain.Users;
using LocalErp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class EfRequestForQuotationRepositoryTests
{
    private static readonly DateTime CreatedAt = new(2026, 10, 21, 13, 30, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task RepositoryPersistsNestedLifecycleAndScaledUnitPrice()
    {
        var testDatabase = await TestDatabase.CreateAsync();
        await using var cleanup = testDatabase;
        var userRepository = new EfUserRepository(testDatabase);
        var unitRepository = new EfMeasurementUnitRepository(testDatabase);
        var supplierRepository = new EfSupplierRepository(testDatabase);
        var purchaseRequestRepository = new EfPurchaseRequestRepository(testDatabase);
        var rfqRepository = new EfRequestForQuotationRepository(testDatabase);

        var user = new User("buyer", "hash");
        var unit = new MeasurementUnit("Peça");
        var supplier = new Supplier("Fornecedor A");
        await userRepository.AddAsync(user);
        await unitRepository.AddAsync(unit);
        await supplierRepository.AddAsync(supplier);

        var source = new PurchaseRequest(user.Id, CreatedAt, "Necessidade");
        var firstSourceLine = source.AddLine("Rolamento", 2m, unit.Id);
        var secondSourceLine = source.AddLine("Parafuso", 10m, unit.Id);
        source.Submit(user.Id, CreatedAt.AddMinutes(5));
        await purchaseRequestRepository.AddAsync(source);

        var rfq = new RequestForQuotation(source.Id, user.Id, CreatedAt.AddMinutes(10), "Cotação inicial");
        var firstRfqLine = rfq.AddSourceLine(firstSourceLine.Id);
        var secondRfqLine = rfq.AddSourceLine(secondSourceLine.Id);
        rfq.AddSupplier(supplier.Id);
        await rfqRepository.AddAsync(rfq);

        var loaded = await rfqRepository.GetByIdAsync(rfq.Id)
            ?? throw new AssertFailedException("Persisted RFQ was not found.");
        loaded.Send(user.Id, CreatedAt.AddMinutes(15));
        var quotation = loaded.CreateSupplierQuotation(
            supplier.Id,
            "BRL",
            user.Id,
            CreatedAt.AddMinutes(20),
            "REF-01");
        var firstQuotedLine = loaded.UpsertSupplierQuotationLine(
            quotation.Id,
            firstRfqLine.Id,
            20.000001m,
            new DateOnly(2026, 11, 10));
        var removedQuotedLine = loaded.UpsertSupplierQuotationLine(
            quotation.Id,
            secondRfqLine.Id,
            3.5m);
        await rfqRepository.UpdateAsync(loaded);

        var withQuotation = await rfqRepository.GetByIdAsync(rfq.Id)
            ?? throw new AssertFailedException("RFQ with supplier quotation was not found.");
        var persistedQuotation = withQuotation.SupplierQuotations.Single();
        withQuotation.RemoveSupplierQuotationLine(persistedQuotation.Id, removedQuotedLine.Id);
        withQuotation.UpsertSupplierQuotationLine(
            persistedQuotation.Id,
            firstRfqLine.Id,
            25.123456m,
            new DateOnly(2026, 11, 12),
            "Preço final");
        withQuotation.FinalizeSupplierQuotation(persistedQuotation.Id, user.Id, CreatedAt.AddMinutes(30));
        withQuotation.Close(user.Id, CreatedAt.AddMinutes(35));
        await rfqRepository.UpdateAsync(withQuotation);

        var final = await rfqRepository.GetByIdAsync(rfq.Id)
            ?? throw new AssertFailedException("Closed RFQ was not found.");
        Assert.AreEqual(RequestForQuotationStatus.Closed, final.Status);
        Assert.AreEqual(2, final.Lines.Count);
        Assert.AreEqual(1, final.Suppliers.Count);
        Assert.AreEqual(1, final.SupplierQuotations.Count);
        var finalQuotation = final.SupplierQuotations.Single();
        Assert.AreEqual(SupplierQuotationStatus.Finalized, finalQuotation.Status);
        Assert.AreEqual("BRL", finalQuotation.CurrencyCode);
        Assert.AreEqual(1, finalQuotation.Lines.Count);
        Assert.AreEqual(firstQuotedLine.Id, finalQuotation.Lines[0].Id);
        Assert.AreEqual(25.123456m, finalQuotation.Lines[0].UnitPrice);
        Assert.AreEqual(new DateOnly(2026, 11, 12), finalQuotation.Lines[0].PromisedDeliveryDate);
        Assert.AreEqual("Preço final", finalQuotation.Lines[0].Notes);

        var closed = await rfqRepository.ListAsync(RequestForQuotationStatus.Closed);
        Assert.AreEqual(1, closed.Count);
        Assert.AreEqual(rfq.Id, closed[0].Id);

        await using var context = testDatabase.CreateDbContext();
        var connection = context.Database.GetDbConnection();
        await connection.OpenAsync();
        await using var command = connection.CreateCommand();
        command.CommandText = "SELECT UnitPrice FROM SupplierQuotationLines;";
        var storedValue = await command.ExecuteScalarAsync();
        Assert.IsNotNull(storedValue);
        Assert.AreEqual(25_123_456L, Convert.ToInt64(storedValue, CultureInfo.InvariantCulture));
    }

    [TestMethod]
    public async Task RepositoryEnforcesSourceAndSupplierForeignKeys()
    {
        var testDatabase = await TestDatabase.CreateAsync();
        await using var cleanup = testDatabase;
        var userRepository = new EfUserRepository(testDatabase);
        var unitRepository = new EfMeasurementUnitRepository(testDatabase);
        var purchaseRequestRepository = new EfPurchaseRequestRepository(testDatabase);
        var rfqRepository = new EfRequestForQuotationRepository(testDatabase);

        var user = new User("buyer", "hash");
        var unit = new MeasurementUnit("Unidade");
        await userRepository.AddAsync(user);
        await unitRepository.AddAsync(unit);

        var orphan = new RequestForQuotation(Guid.NewGuid(), user.Id, CreatedAt);
        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => rfqRepository.AddAsync(orphan));

        var source = new PurchaseRequest(user.Id, CreatedAt);
        var sourceLine = source.AddLine("Item", 1m, unit.Id);
        source.Submit(user.Id, CreatedAt.AddMinutes(1));
        await purchaseRequestRepository.AddAsync(source);

        var invalidSupplierRfq = new RequestForQuotation(source.Id, user.Id, CreatedAt.AddMinutes(2));
        invalidSupplierRfq.AddSourceLine(sourceLine.Id);
        invalidSupplierRfq.AddSupplier(Guid.NewGuid());
        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => rfqRepository.AddAsync(invalidSupplierRfq));
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
            var databasePath = Path.Combine(directory, "request-for-quotations.db");
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
