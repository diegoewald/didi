using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Suppliers;
using LocalErp.Domain.Units;
using LocalErp.Domain.Users;
using LocalErp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class SqlitePurchaseOrderRepositoryTests
{
    private static readonly DateTime Now = new(2026, 10, 22, 15, 30, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task RepositoryReservesSequencePersistsSnapshotsAndUpdatesLifecycle()
    {
        var testDatabase = await TestDatabase.CreateAsync();
        await using var cleanup = testDatabase;
        var fixture = await CreateFinalizedAwardAsync(testDatabase, "purchase-order-roundtrip");
        var repository = new SqlitePurchaseOrderRepository(testDatabase);

        var created = await repository.AddWithNextSequenceAsync(sequenceNumber =>
            CreatePurchaseOrder(sequenceNumber, fixture));

        Assert.AreEqual(1, created.SequenceNumber);

        var persisted = await repository.GetByIdAsync(created.Id)
            ?? throw new AssertFailedException("Persisted purchase order was not found.");
        Assert.AreEqual(1, persisted.SequenceNumber);
        Assert.AreEqual(fixture.Award.Id, persisted.RequestForQuotationAwardId);
        Assert.AreEqual(fixture.Supplier.Id, persisted.SupplierId);
        Assert.AreEqual(fixture.Supplier.Name, persisted.SupplierNameSnapshot);
        Assert.AreEqual("BRL", persisted.CurrencyCode);
        Assert.AreEqual("observação inicial", persisted.Notes);
        Assert.AreEqual(1, persisted.Lines.Count);
        Assert.AreEqual(fixture.AwardLine.Id, persisted.Lines[0].RequestForQuotationAwardLineId);
        Assert.AreEqual("Item de precisão", persisted.Lines[0].DescriptionSnapshot);
        Assert.AreEqual(2.5m, persisted.Lines[0].Quantity);
        Assert.AreEqual(fixture.Unit.Id, persisted.Lines[0].MeasurementUnitId);
        Assert.AreEqual(fixture.Unit.Name, persisted.Lines[0].MeasurementUnitNameSnapshot);
        Assert.AreEqual(12.345678m, persisted.Lines[0].UnitPrice);
        Assert.AreEqual(new DateOnly(2026, 11, 15), persisted.Lines[0].PromisedDeliveryDate);
        Assert.AreEqual(30.8641950m, persisted.Total);

        persisted.UpdateNotes("  revisado  ");
        persisted.Issue(fixture.User.Id, Now.AddMinutes(20));
        await repository.UpdateAsync(persisted);

        var issued = await repository.GetByAwardAndSupplierAsync(fixture.Award.Id, fixture.Supplier.Id)
            ?? throw new AssertFailedException("Issued purchase order was not found by source.");
        Assert.AreEqual(PurchaseOrderStatus.Issued, issued.Status);
        Assert.AreEqual("revisado", issued.Notes);
        Assert.AreEqual(fixture.User.Id, issued.IssuedByUserId);
        Assert.AreEqual(Now.AddMinutes(20), issued.IssuedAtUtc);

        issued.Cancel(fixture.User.Id, Now.AddMinutes(30), "  necessidade cancelada  ");
        await repository.UpdateAsync(issued);

        var cancelled = await repository.GetByIdAsync(created.Id)
            ?? throw new AssertFailedException("Cancelled purchase order was not found.");
        Assert.AreEqual(PurchaseOrderStatus.Cancelled, cancelled.Status);
        Assert.AreEqual("necessidade cancelada", cancelled.CancellationReason);
        Assert.AreEqual(fixture.User.Id, cancelled.CancelledByUserId);
    }

    [TestMethod]
    public async Task ConcurrentCreationReservesDistinctIncreasingSequences()
    {
        var testDatabase = await TestDatabase.CreateAsync();
        await using var cleanup = testDatabase;
        var firstFixture = await CreateFinalizedAwardAsync(testDatabase, "purchase-order-concurrency-a");
        var secondFixture = await CreateFinalizedAwardAsync(testDatabase, "purchase-order-concurrency-b");
        var repository = new SqlitePurchaseOrderRepository(testDatabase);

        var results = await Task.WhenAll(
            Task.Run(() => repository.AddWithNextSequenceAsync(
                sequenceNumber => CreatePurchaseOrder(sequenceNumber, firstFixture))),
            Task.Run(() => repository.AddWithNextSequenceAsync(
                sequenceNumber => CreatePurchaseOrder(sequenceNumber, secondFixture))));

        CollectionAssert.AreEquivalent(new[] { 1, 2 }, results.Select(item => item.SequenceNumber).ToArray());

        var persisted = await repository.ListAsync();
        CollectionAssert.AreEqual(new[] { 2, 1 }, persisted.Select(item => item.SequenceNumber).ToArray());
    }

    [TestMethod]
    public async Task DatabaseRejectsDuplicateAwardSupplierAndAwardLineConversion()
    {
        var testDatabase = await TestDatabase.CreateAsync();
        await using var cleanup = testDatabase;
        var fixture = await CreateFinalizedAwardAsync(testDatabase, "purchase-order-uniqueness");
        var repository = new SqlitePurchaseOrderRepository(testDatabase);

        await repository.AddWithNextSequenceAsync(sequenceNumber =>
            CreatePurchaseOrder(sequenceNumber, fixture));

        await Assert.ThrowsExactlyAsync<DbUpdateException>(() =>
            repository.AddWithNextSequenceAsync(sequenceNumber =>
                CreatePurchaseOrder(sequenceNumber, fixture)));

        var alternateSupplier = new Supplier("Fornecedor alternativo");
        await new EfSupplierRepository(testDatabase).AddAsync(alternateSupplier);
        await Assert.ThrowsExactlyAsync<DbUpdateException>(() =>
            repository.AddWithNextSequenceAsync(sequenceNumber =>
                new PurchaseOrder(
                    sequenceNumber,
                    fixture.Award.Id,
                    alternateSupplier.Id,
                    alternateSupplier.Name,
                    "BRL",
                    fixture.User.Id,
                    Now,
                    [CreateSnapshot(fixture)])));
    }

    [TestMethod]
    public async Task DatabaseRejectsMissingExternalReferences()
    {
        var testDatabase = await TestDatabase.CreateAsync();
        await using var cleanup = testDatabase;
        var fixture = await CreateFinalizedAwardAsync(testDatabase, "purchase-order-fks");
        var repository = new SqlitePurchaseOrderRepository(testDatabase);

        await Assert.ThrowsExactlyAsync<DbUpdateException>(() =>
            repository.AddWithNextSequenceAsync(sequenceNumber =>
                new PurchaseOrder(
                    sequenceNumber,
                    Guid.NewGuid(),
                    fixture.Supplier.Id,
                    fixture.Supplier.Name,
                    "BRL",
                    fixture.User.Id,
                    Now,
                    [CreateSnapshot(fixture)])));

        await Assert.ThrowsExactlyAsync<DbUpdateException>(() =>
            repository.AddWithNextSequenceAsync(sequenceNumber =>
                new PurchaseOrder(
                    sequenceNumber,
                    fixture.Award.Id,
                    Guid.NewGuid(),
                    "Fornecedor inexistente",
                    "BRL",
                    fixture.User.Id,
                    Now,
                    [CreateSnapshot(fixture)])));
    }

    private static PurchaseOrder CreatePurchaseOrder(int sequenceNumber, Fixture fixture) =>
        new(
            sequenceNumber,
            fixture.Award.Id,
            fixture.Supplier.Id,
            fixture.Supplier.Name,
            "BRL",
            fixture.User.Id,
            Now,
            [CreateSnapshot(fixture)],
            "  observação inicial  ");

    private static PurchaseOrderLineSnapshot CreateSnapshot(Fixture fixture) =>
        new(
            fixture.AwardLine.Id,
            "Item de precisão",
            2.5m,
            fixture.Unit.Id,
            fixture.Unit.Name,
            12.345678m,
            new DateOnly(2026, 11, 15));

    private static async Task<Fixture> CreateFinalizedAwardAsync(
        TestDatabase testDatabase,
        string suffix)
    {
        var userRepository = new EfUserRepository(testDatabase);
        var unitRepository = new EfMeasurementUnitRepository(testDatabase);
        var supplierRepository = new EfSupplierRepository(testDatabase);
        var purchaseRequestRepository = new EfPurchaseRequestRepository(testDatabase);
        var rfqRepository = new EfRequestForQuotationRepository(testDatabase);
        var awardRepository = new EfRequestForQuotationAwardRepository(testDatabase);

        var user = new User($"buyer-{suffix}", "hash");
        var unit = new MeasurementUnit($"UN-{suffix}");
        var supplier = new Supplier($"Fornecedor {suffix}");
        await userRepository.AddAsync(user);
        await unitRepository.AddAsync(unit);
        await supplierRepository.AddAsync(supplier);

        var purchaseRequest = new PurchaseRequest(user.Id, Now, $"Origem {suffix}");
        var sourceLine = purchaseRequest.AddLine("Item de precisão", 2.5m, unit.Id);
        purchaseRequest.Submit(user.Id, Now.AddMinutes(1));
        await purchaseRequestRepository.AddAsync(purchaseRequest);

        var rfq = new RequestForQuotation(purchaseRequest.Id, user.Id, Now.AddMinutes(2));
        var rfqLine = rfq.AddSourceLine(sourceLine.Id);
        rfq.AddSupplier(supplier.Id);
        rfq.Send(user.Id, Now.AddMinutes(3));
        var quotation = rfq.CreateSupplierQuotation(
            supplier.Id,
            "BRL",
            user.Id,
            Now.AddMinutes(4));
        var quotationLine = rfq.UpsertSupplierQuotationLine(
            quotation.Id,
            rfqLine.Id,
            12.345678m,
            new DateOnly(2026, 11, 15));
        rfq.FinalizeSupplierQuotation(quotation.Id, user.Id, Now.AddMinutes(5));
        rfq.Close(user.Id, Now.AddMinutes(6));
        await rfqRepository.AddAsync(rfq);

        var award = new RequestForQuotationAward(rfq.Id, user.Id, Now.AddMinutes(7));
        var awardLine = award.SelectLine(rfqLine.Id, quotationLine.Id);
        award.Finalize(user.Id, Now.AddMinutes(8));
        await awardRepository.AddAsync(award);

        return new Fixture(user, unit, supplier, award, awardLine);
    }

    private sealed record Fixture(
        User User,
        MeasurementUnit Unit,
        Supplier Supplier,
        RequestForQuotationAward Award,
        RequestForQuotationAwardLine AwardLine);

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
            var databasePath = Path.Combine(directory, "purchase-orders.db");
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
