using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Purchasing;
using LocalErp.Application.Security;
using LocalErp.Application.Users;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Security;
using LocalErp.Domain.Suppliers;
using LocalErp.Domain.Units;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class PurchaseOrderServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 22, 14, 30, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task CreateDraftBuildsTrustedSnapshotsFromFinalizedAward()
    {
        var fixture = CreateFixture();
        fixture.SecondUnit.Deactivate();
        var authorization = new RecordingAuthorizationService();
        var orders = new InMemoryPurchaseOrderRepository();
        var service = NewService(fixture, orders, authorization: authorization);

        var created = await service.CreateDraftFromAwardAsync(
            fixture.Award.Id,
            fixture.SupplierA.Id,
            "  revisar antes de emitir  ");

        Assert.AreEqual(1, created.SequenceNumber);
        Assert.AreEqual(fixture.Award.Id, created.RequestForQuotationAwardId);
        Assert.AreEqual(fixture.SupplierA.Id, created.SupplierId);
        Assert.AreEqual(fixture.SupplierA.Name, created.SupplierNameSnapshot);
        Assert.AreEqual("BRL", created.CurrencyCode);
        Assert.AreEqual(PurchaseOrderStatus.Draft, created.Status);
        Assert.AreEqual("revisar antes de emitir", created.Notes);
        Assert.AreEqual(2, created.Lines.Count);
        Assert.AreEqual("Rolamento", created.Lines[0].DescriptionSnapshot);
        Assert.AreEqual(2m, created.Lines[0].Quantity);
        Assert.AreEqual(fixture.FirstUnit.Id, created.Lines[0].MeasurementUnitId);
        Assert.AreEqual("UN", created.Lines[0].MeasurementUnitNameSnapshot);
        Assert.AreEqual(10.25m, created.Lines[0].UnitPrice);
        Assert.AreEqual(new DateOnly(2026, 11, 5), created.Lines[0].PromisedDeliveryDate);
        Assert.AreEqual("Correia", created.Lines[1].DescriptionSnapshot);
        Assert.AreEqual(fixture.SecondUnit.Id, created.Lines[1].MeasurementUnitId);
        Assert.AreEqual("CX", created.Lines[1].MeasurementUnitNameSnapshot);
        Assert.AreEqual(4m, created.Lines[1].UnitPrice);
        Assert.AreEqual(32.50m, created.Total);
        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.PurchaseOrdersManage },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task OneOrderPerAwardAndSupplierUsesIndependentSequences()
    {
        var fixture = CreateFixture();
        var orders = new InMemoryPurchaseOrderRepository();
        var service = NewService(fixture, orders);

        var first = await service.CreateDraftFromAwardAsync(fixture.Award.Id, fixture.SupplierA.Id);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.CreateDraftFromAwardAsync(fixture.Award.Id, fixture.SupplierA.Id));
        var second = await service.CreateDraftFromAwardAsync(fixture.Award.Id, fixture.SupplierB.Id);

        Assert.AreEqual(1, first.SequenceNumber);
        Assert.AreEqual(2, second.SequenceNumber);
        Assert.AreEqual("USD", second.CurrencyCode);
        Assert.AreEqual(1, second.Lines.Count);
        Assert.AreEqual("Sensor", second.Lines[0].DescriptionSnapshot);
        Assert.AreEqual(30m, second.Total);
    }

    [TestMethod]
    public async Task CreationRequiresFinalizedAwardAndActiveSupplier()
    {
        var fixture = CreateFixture();
        var draftAward = new RequestForQuotationAward(
            fixture.RequestForQuotation.Id,
            Guid.NewGuid(),
            Now);
        var awards = new InMemoryRequestForQuotationAwardRepository([draftAward]);
        var service = NewService(fixture, awards: awards);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.CreateDraftFromAwardAsync(draftAward.Id, fixture.SupplierA.Id));

        fixture.SupplierA.Deactivate();
        service = NewService(fixture);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.CreateDraftFromAwardAsync(fixture.Award.Id, fixture.SupplierA.Id));
    }

    [TestMethod]
    public async Task IssueRevalidatesSupplierAndCancelPreservesIssuedHistory()
    {
        var fixture = CreateFixture();
        var orders = new InMemoryPurchaseOrderRepository();
        var actor = new UserResult(Guid.NewGuid(), "buyer", true);
        var service = NewService(
            fixture,
            orders,
            session: new TestSession(actor));
        var created = await service.CreateDraftFromAwardAsync(fixture.Award.Id, fixture.SupplierA.Id);

        fixture.SupplierA.Deactivate();
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.IssueAsync(created.Id));
        fixture.SupplierA.Activate();

        var issued = await service.IssueAsync(created.Id);
        Assert.AreEqual(PurchaseOrderStatus.Issued, issued.Status);
        Assert.AreEqual(actor.Id, issued.IssuedByUserId);
        Assert.AreEqual(Now, issued.IssuedAtUtc);

        fixture.SupplierA.Deactivate();
        var cancelled = await service.CancelAsync(created.Id, "  necessidade cancelada  ");
        Assert.AreEqual(PurchaseOrderStatus.Cancelled, cancelled.Status);
        Assert.AreEqual(actor.Id, cancelled.CancelledByUserId);
        Assert.AreEqual("necessidade cancelada", cancelled.CancellationReason);
    }

    [TestMethod]
    public async Task ReadsUseReadPermissionAndMutationsRequireActiveSession()
    {
        var fixture = CreateFixture();
        var orders = new InMemoryPurchaseOrderRepository();
        var authorization = new RecordingAuthorizationService();
        var service = NewService(fixture, orders, authorization: authorization);
        var created = await service.CreateDraftFromAwardAsync(fixture.Award.Id, fixture.SupplierA.Id);
        authorization.Demands.Clear();

        var listed = await service.ListAsync(PurchaseOrderStatus.Draft);
        var byId = await service.GetByIdAsync(created.Id);

        Assert.AreEqual(1, listed.Count);
        Assert.AreEqual(created.Id, byId.Id);
        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.PurchaseOrdersRead, PermissionCatalog.PurchaseOrdersRead },
            authorization.Demands.ToArray());

        var withoutSession = NewService(
            fixture,
            orders,
            session: new TestSession(null));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            withoutSession.UpdateDraftAsync(created.Id, "nova observação"));

        var inactiveSession = NewService(
            fixture,
            orders,
            session: new TestSession(new UserResult(Guid.NewGuid(), "inactive", false)));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            inactiveSession.CancelAsync(created.Id, "motivo"));
    }

    private static PurchaseOrderService NewService(
        Fixture fixture,
        InMemoryPurchaseOrderRepository? orders = null,
        RecordingAuthorizationService? authorization = null,
        TestSession? session = null,
        InMemoryRequestForQuotationAwardRepository? awards = null)
    {
        return new PurchaseOrderService(
            orders ?? new InMemoryPurchaseOrderRepository(),
            awards ?? new InMemoryRequestForQuotationAwardRepository([fixture.Award]),
            new InMemoryRequestForQuotationRepository([fixture.RequestForQuotation]),
            new InMemoryPurchaseRequestRepository([fixture.PurchaseRequest]),
            new InMemorySupplierRepository([fixture.SupplierA, fixture.SupplierB]),
            new InMemoryMeasurementUnitRepository([fixture.FirstUnit, fixture.SecondUnit]),
            authorization ?? new RecordingAuthorizationService(),
            session ?? new TestSession(new UserResult(Guid.NewGuid(), "buyer", true)),
            new TestClock(Now));
    }

    private static Fixture CreateFixture()
    {
        var firstUnit = new MeasurementUnit("UN");
        var secondUnit = new MeasurementUnit("CX");
        var purchaseRequest = new PurchaseRequest(Guid.NewGuid(), Now);
        var firstPurchaseLine = purchaseRequest.AddLine("Rolamento", 2m, firstUnit.Id);
        var secondPurchaseLine = purchaseRequest.AddLine("Correia", 3m, secondUnit.Id);
        var thirdPurchaseLine = purchaseRequest.AddLine("Sensor", 1m, firstUnit.Id);
        purchaseRequest.Submit(Guid.NewGuid(), Now);

        var supplierA = new Supplier("Fornecedor A");
        var supplierB = new Supplier("Fornecedor B");
        var requestForQuotation = new RequestForQuotation(
            purchaseRequest.Id,
            Guid.NewGuid(),
            Now);
        var firstRfqLine = requestForQuotation.AddSourceLine(firstPurchaseLine.Id);
        var secondRfqLine = requestForQuotation.AddSourceLine(secondPurchaseLine.Id);
        var thirdRfqLine = requestForQuotation.AddSourceLine(thirdPurchaseLine.Id);
        requestForQuotation.AddSupplier(supplierA.Id);
        requestForQuotation.AddSupplier(supplierB.Id);
        requestForQuotation.Send(Guid.NewGuid(), Now);

        var quotationA = requestForQuotation.CreateSupplierQuotation(
            supplierA.Id,
            "BRL",
            Guid.NewGuid(),
            Now);
        var quotationALine1 = requestForQuotation.UpsertSupplierQuotationLine(
            quotationA.Id,
            firstRfqLine.Id,
            10.25m,
            new DateOnly(2026, 11, 5));
        var quotationALine2 = requestForQuotation.UpsertSupplierQuotationLine(
            quotationA.Id,
            secondRfqLine.Id,
            4m,
            new DateOnly(2026, 11, 7));
        requestForQuotation.FinalizeSupplierQuotation(quotationA.Id, Guid.NewGuid(), Now);

        var quotationB = requestForQuotation.CreateSupplierQuotation(
            supplierB.Id,
            "USD",
            Guid.NewGuid(),
            Now);
        var quotationBLine = requestForQuotation.UpsertSupplierQuotationLine(
            quotationB.Id,
            thirdRfqLine.Id,
            30m,
            new DateOnly(2026, 11, 10));
        requestForQuotation.FinalizeSupplierQuotation(quotationB.Id, Guid.NewGuid(), Now);
        requestForQuotation.Close(Guid.NewGuid(), Now);

        var award = new RequestForQuotationAward(
            requestForQuotation.Id,
            Guid.NewGuid(),
            Now);
        award.SelectLine(firstRfqLine.Id, quotationALine1.Id);
        award.SelectLine(secondRfqLine.Id, quotationALine2.Id);
        award.SelectLine(thirdRfqLine.Id, quotationBLine.Id);
        award.Finalize(Guid.NewGuid(), Now);

        return new Fixture(
            purchaseRequest,
            requestForQuotation,
            award,
            supplierA,
            supplierB,
            firstUnit,
            secondUnit);
    }

    private sealed record Fixture(
        PurchaseRequest PurchaseRequest,
        RequestForQuotation RequestForQuotation,
        RequestForQuotationAward Award,
        Supplier SupplierA,
        Supplier SupplierB,
        MeasurementUnit FirstUnit,
        MeasurementUnit SecondUnit);

    private sealed class RecordingAuthorizationService : IAuthorizationService
    {
        public List<string> Demands { get; } = [];

        public Task<bool> IsAllowedAsync(string permissionCode, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }

        public Task DemandAsync(string permissionCode, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Demands.Add(permissionCode);
            return Task.CompletedTask;
        }
    }

    private sealed class TestSession(UserResult? currentUser) : IUserSession
    {
        public bool IsAuthenticated => CurrentUser is not null;

        public UserResult? CurrentUser { get; private set; } = currentUser;

        public void SignIn(UserResult user) => CurrentUser = user;

        public void SignOut() => CurrentUser = null;
    }

    private sealed class TestClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class InMemoryPurchaseOrderRepository : IPurchaseOrderRepository
    {
        private int _nextSequenceNumber = 1;

        public List<PurchaseOrder> Items { get; } = [];

        public Task<PurchaseOrder> AddWithNextSequenceAsync(
            Func<int, PurchaseOrder> purchaseOrderFactory,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(purchaseOrderFactory);
            var purchaseOrder = purchaseOrderFactory(_nextSequenceNumber++);
            if (Items.Any(item =>
                item.RequestForQuotationAwardId == purchaseOrder.RequestForQuotationAwardId &&
                item.SupplierId == purchaseOrder.SupplierId))
            {
                throw new InvalidOperationException("Duplicate purchase order source.");
            }

            Items.Add(purchaseOrder);
            return Task.FromResult(purchaseOrder);
        }

        public Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        }

        public Task<PurchaseOrder?> GetByAwardAndSupplierAsync(
            Guid requestForQuotationAwardId,
            Guid supplierId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item =>
                item.RequestForQuotationAwardId == requestForQuotationAwardId &&
                item.SupplierId == supplierId));
        }

        public Task<IReadOnlyList<PurchaseOrder>> ListAsync(
            PurchaseOrderStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<PurchaseOrder> result = Items
                .Where(item => status is null || item.Status == status)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRequestForQuotationAwardRepository(
        IEnumerable<RequestForQuotationAward> items) : IRequestForQuotationAwardRepository
    {
        public List<RequestForQuotationAward> Items { get; } = [.. items];

        public Task AddAsync(RequestForQuotationAward award, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Items.Add(award);
            return Task.CompletedTask;
        }

        public Task<RequestForQuotationAward?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        }

        public Task<RequestForQuotationAward?> GetByRequestForQuotationIdAsync(
            Guid requestForQuotationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(
                item => item.RequestForQuotationId == requestForQuotationId));
        }

        public Task UpdateAsync(RequestForQuotationAward award, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRequestForQuotationRepository(
        IEnumerable<RequestForQuotation> items) : IRequestForQuotationRepository
    {
        public List<RequestForQuotation> Items { get; } = [.. items];

        public Task AddAsync(RequestForQuotation requestForQuotation, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Items.Add(requestForQuotation);
            return Task.CompletedTask;
        }

        public Task<RequestForQuotation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        }

        public Task<IReadOnlyList<RequestForQuotation>> ListAsync(
            RequestForQuotationStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<RequestForQuotation> result = Items
                .Where(item => status is null || item.Status == status)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(RequestForQuotation requestForQuotation, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryPurchaseRequestRepository(
        IEnumerable<PurchaseRequest> items) : IPurchaseRequestRepository
    {
        public List<PurchaseRequest> Items { get; } = [.. items];

        public Task AddAsync(PurchaseRequest purchaseRequest, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Items.Add(purchaseRequest);
            return Task.CompletedTask;
        }

        public Task<PurchaseRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        }

        public Task<IReadOnlyList<PurchaseRequest>> ListAsync(
            PurchaseRequestStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<PurchaseRequest> result = Items
                .Where(item => status is null || item.Status == status)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(PurchaseRequest purchaseRequest, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class InMemorySupplierRepository(
        IEnumerable<Supplier> items) : ISupplierRepository
    {
        public List<Supplier> Items { get; } = [.. items];

        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Items.Add(supplier);
            return Task.CompletedTask;
        }

        public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        }

        public Task<IReadOnlyList<Supplier>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<Supplier> result = Items
                .Where(item => includeInactive || item.IsActive)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryMeasurementUnitRepository(
        IEnumerable<MeasurementUnit> items) : IMeasurementUnitRepository
    {
        public List<MeasurementUnit> Items { get; } = [.. items];

        public Task AddAsync(MeasurementUnit unit, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Items.Add(unit);
            return Task.CompletedTask;
        }

        public Task<MeasurementUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        }

        public Task<IReadOnlyList<MeasurementUnit>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<MeasurementUnit> result = Items
                .Where(item => includeInactive || item.IsActive)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(MeasurementUnit unit, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
