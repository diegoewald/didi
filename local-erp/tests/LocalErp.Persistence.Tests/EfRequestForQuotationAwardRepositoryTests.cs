using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Suppliers;
using LocalErp.Domain.Units;
using LocalErp.Domain.Users;
using LocalErp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class EfRequestForQuotationAwardRepositoryTests
{
    private static readonly DateTime Now = new(2026, 10, 22, 11, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task RepositoryPersistsUpdatesAndFinalizesAwardDecision()
    {
        var testDatabase = await TestDatabase.CreateAsync();
        await using var cleanup = testDatabase;
        var fixture = await CreateClosedRfqAsync(testDatabase, "award-roundtrip");
        var repository = new EfRequestForQuotationAwardRepository(testDatabase);

        var award = new RequestForQuotationAward(
            fixture.RequestForQuotation.Id,
            fixture.User.Id,
            Now,
            "  decisão inicial  ");
        award.SelectLine(fixture.FirstRfqLineId, fixture.FirstSupplierQuotationLineId);
        award.SelectLine(fixture.SecondRfqLineId, fixture.SecondSupplierQuotationLineId);
        await repository.AddAsync(award);

        var byId = await repository.GetByIdAsync(award.Id)
            ?? throw new AssertFailedException("Persisted RFQ award decision was not found by id.");
        var byRfq = await repository.GetByRequestForQuotationIdAsync(fixture.RequestForQuotation.Id)
            ?? throw new AssertFailedException("Persisted RFQ award decision was not found by RFQ id.");
        Assert.AreEqual(award.Id, byRfq.Id);
        Assert.AreEqual("decisão inicial", byId.Notes);
        Assert.AreEqual(2, byId.Lines.Count);

        byId.UpdateNotes("decisão final");
        byId.RemoveLineSelection(fixture.SecondRfqLineId);
        byId.SelectLine(fixture.FirstRfqLineId, fixture.AlternateFirstSupplierQuotationLineId);
        byId.Finalize(fixture.User.Id, Now.AddMinutes(10));
        await repository.UpdateAsync(byId);

        var final = await repository.GetByIdAsync(award.Id)
            ?? throw new AssertFailedException("Updated RFQ award decision was not found.");
        Assert.AreEqual(RequestForQuotationAwardStatus.Finalized, final.Status);
        Assert.AreEqual("decisão final", final.Notes);
        Assert.AreEqual(fixture.User.Id, final.FinalizedByUserId);
        Assert.AreEqual(Now.AddMinutes(10), final.FinalizedAtUtc);
        Assert.AreEqual(1, final.Lines.Count);
        Assert.AreEqual(fixture.FirstRfqLineId, final.Lines[0].RequestForQuotationLineId);
        Assert.AreEqual(
            fixture.AlternateFirstSupplierQuotationLineId,
            final.Lines[0].SupplierQuotationLineId);
    }

    [TestMethod]
    public async Task DatabaseRejectsSecondAwardForSameRfq()
    {
        var testDatabase = await TestDatabase.CreateAsync();
        await using var cleanup = testDatabase;
        var fixture = await CreateClosedRfqAsync(testDatabase, "award-unique-rfq");
        var repository = new EfRequestForQuotationAwardRepository(testDatabase);

        var first = new RequestForQuotationAward(
            fixture.RequestForQuotation.Id,
            fixture.User.Id,
            Now);
        await repository.AddAsync(first);

        var duplicate = new RequestForQuotationAward(
            fixture.RequestForQuotation.Id,
            fixture.User.Id,
            Now.AddMinutes(1));

        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => repository.AddAsync(duplicate));
    }

    [TestMethod]
    public async Task DatabaseRejectsMissingExternalAwardReferences()
    {
        var testDatabase = await TestDatabase.CreateAsync();
        await using var cleanup = testDatabase;
        var fixture = await CreateClosedRfqAsync(testDatabase, "award-fks");
        var repository = new EfRequestForQuotationAwardRepository(testDatabase);

        var orphanAward = new RequestForQuotationAward(
            Guid.NewGuid(),
            fixture.User.Id,
            Now);
        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => repository.AddAsync(orphanAward));

        var invalidLineReferences = new RequestForQuotationAward(
            fixture.RequestForQuotation.Id,
            fixture.User.Id,
            Now.AddMinutes(1));
        invalidLineReferences.SelectLine(Guid.NewGuid(), Guid.NewGuid());
        await Assert.ThrowsExactlyAsync<DbUpdateException>(() => repository.AddAsync(invalidLineReferences));
    }

    private static async Task<ClosedRfqFixture> CreateClosedRfqAsync(
        TestDatabase testDatabase,
        string username)
    {
        var userRepository = new EfUserRepository(testDatabase);
        var unitRepository = new EfMeasurementUnitRepository(testDatabase);
        var supplierRepository = new EfSupplierRepository(testDatabase);
        var purchaseRequestRepository = new EfPurchaseRequestRepository(testDatabase);
        var rfqRepository = new EfRequestForQuotationRepository(testDatabase);

        var user = new User(username, "hash");
        var unit = new MeasurementUnit("Unidade");
        var firstSupplier = new Supplier("Fornecedor A");
        var secondSupplier = new Supplier("Fornecedor B");
        await userRepository.AddAsync(user);
        await unitRepository.AddAsync(unit);
        await supplierRepository.AddAsync(firstSupplier);
        await supplierRepository.AddAsync(secondSupplier);

        var source = new PurchaseRequest(user.Id, Now, "Origem da adjudicação");
        var firstSourceLine = source.AddLine("Item A", 2m, unit.Id);
        var secondSourceLine = source.AddLine("Item B", 3m, unit.Id);
        source.Submit(user.Id, Now.AddMinutes(1));
        await purchaseRequestRepository.AddAsync(source);

        var rfq = new RequestForQuotation(source.Id, user.Id, Now.AddMinutes(2));
        var firstRfqLine = rfq.AddSourceLine(firstSourceLine.Id);
        var secondRfqLine = rfq.AddSourceLine(secondSourceLine.Id);
        rfq.AddSupplier(firstSupplier.Id);
        rfq.AddSupplier(secondSupplier.Id);
        rfq.Send(user.Id, Now.AddMinutes(3));

        var firstQuotation = rfq.CreateSupplierQuotation(
            firstSupplier.Id,
            "BRL",
            user.Id,
            Now.AddMinutes(4));
        var firstQuotationLine = rfq.UpsertSupplierQuotationLine(
            firstQuotation.Id,
            firstRfqLine.Id,
            12.5m);
        var secondQuotationLine = rfq.UpsertSupplierQuotationLine(
            firstQuotation.Id,
            secondRfqLine.Id,
            8m);
        rfq.FinalizeSupplierQuotation(firstQuotation.Id, user.Id, Now.AddMinutes(5));

        var alternateQuotation = rfq.CreateSupplierQuotation(
            secondSupplier.Id,
            "BRL",
            user.Id,
            Now.AddMinutes(6));
        var alternateFirstQuotationLine = rfq.UpsertSupplierQuotationLine(
            alternateQuotation.Id,
            firstRfqLine.Id,
            11.75m);
        rfq.FinalizeSupplierQuotation(alternateQuotation.Id, user.Id, Now.AddMinutes(7));
        rfq.Close(user.Id, Now.AddMinutes(8));
        await rfqRepository.AddAsync(rfq);

        return new ClosedRfqFixture(
            user,
            rfq,
            firstRfqLine.Id,
            secondRfqLine.Id,
            firstQuotationLine.Id,
            secondQuotationLine.Id,
            alternateFirstQuotationLine.Id);
    }

    private sealed record ClosedRfqFixture(
        User User,
        RequestForQuotation RequestForQuotation,
        Guid FirstRfqLineId,
        Guid SecondRfqLineId,
        Guid FirstSupplierQuotationLineId,
        Guid SecondSupplierQuotationLineId,
        Guid AlternateFirstSupplierQuotationLineId);

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
            var databasePath = Path.Combine(directory, "request-for-quotation-awards.db");
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
