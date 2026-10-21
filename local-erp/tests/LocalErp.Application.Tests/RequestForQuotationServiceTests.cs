using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Purchasing;
using LocalErp.Application.Security;
using LocalErp.Application.Users;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Security;
using LocalErp.Domain.Suppliers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class RequestForQuotationServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 13, 30, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task CreateDraftRequiresSubmittedPurchaseRequestAndManagePermission()
    {
        var purchaseRequests = new InMemoryPurchaseRequestRepository();
        var draft = new PurchaseRequest(Guid.NewGuid(), Now);
        await purchaseRequests.AddAsync(draft);
        var authorization = new RecordingAuthorizationService();
        var service = NewService(purchaseRequests: purchaseRequests, authorization: authorization);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.CreateDraftAsync(draft.Id));

        draft.AddLine("Item", 1m, Guid.NewGuid());
        draft.Submit(Guid.NewGuid(), Now);
        var result = await service.CreateDraftAsync(draft.Id, "  cotar  ");

        Assert.AreEqual(RequestForQuotationStatus.Draft, result.Status);
        Assert.AreEqual("cotar", result.Notes);
        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.RequestForQuotationsManage, PermissionCatalog.RequestForQuotationsManage },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task ListAndGetRequireReadPermission()
    {
        var repository = new InMemoryRequestForQuotationRepository();
        var item = new RequestForQuotation(Guid.NewGuid(), Guid.NewGuid(), Now);
        await repository.AddAsync(item);
        var authorization = new RecordingAuthorizationService();
        var service = NewService(repository: repository, authorization: authorization);

        await service.ListAsync();
        await service.GetByIdAsync(item.Id);

        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.RequestForQuotationsRead, PermissionCatalog.RequestForQuotationsRead },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task AddSourceLineRejectsLineFromAnotherPurchaseRequest()
    {
        var purchaseRequests = new InMemoryPurchaseRequestRepository();
        var source = SubmittedPurchaseRequest();
        var other = SubmittedPurchaseRequest();
        await purchaseRequests.AddAsync(source);
        await purchaseRequests.AddAsync(other);
        var repository = new InMemoryRequestForQuotationRepository();
        var rfq = new RequestForQuotation(source.Id, Guid.NewGuid(), Now);
        await repository.AddAsync(rfq);
        var service = NewService(repository, purchaseRequests: purchaseRequests);

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => service.AddSourceLineAsync(
            rfq.Id,
            other.Lines[0].Id));
    }

    [TestMethod]
    public async Task AddSupplierRequiresExistingActiveSupplier()
    {
        var suppliers = new InMemorySupplierRepository();
        var inactive = new Supplier("Fornecedor");
        inactive.Deactivate();
        await suppliers.AddAsync(inactive);
        var repository = new InMemoryRequestForQuotationRepository();
        var rfq = new RequestForQuotation(Guid.NewGuid(), Guid.NewGuid(), Now);
        await repository.AddAsync(rfq);
        var service = NewService(repository, suppliers: suppliers);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.AddSupplierAsync(rfq.Id, inactive.Id));
        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => service.AddSupplierAsync(rfq.Id, Guid.NewGuid()));
    }

    [TestMethod]
    public async Task FullRfqLifecyclePersistsThroughApplicationService()
    {
        var purchaseRequests = new InMemoryPurchaseRequestRepository();
        var source = SubmittedPurchaseRequest();
        await purchaseRequests.AddAsync(source);
        var suppliers = new InMemorySupplierRepository();
        var supplier = new Supplier("Fornecedor A");
        await suppliers.AddAsync(supplier);
        var repository = new InMemoryRequestForQuotationRepository();
        var user = new UserResult(Guid.NewGuid(), "buyer", true);
        var service = NewService(
            repository,
            purchaseRequests,
            suppliers,
            session: new TestSession(user));

        var created = await service.CreateDraftAsync(source.Id);
        var withLine = await service.AddSourceLineAsync(created.Id, source.Lines[0].Id);
        await service.AddSupplierAsync(created.Id, supplier.Id);
        var sent = await service.SendAsync(created.Id);
        var withQuotation = await service.CreateSupplierQuotationAsync(sent.Id, supplier.Id, "brl");
        var quotation = withQuotation.SupplierQuotations.Single();
        var rfqLine = withLine.Lines.Single();
        await service.UpsertSupplierQuotationLineAsync(sent.Id, quotation.Id, rfqLine.Id, 25.123456m);
        await service.FinalizeSupplierQuotationAsync(sent.Id, quotation.Id);
        var closed = await service.CloseAsync(sent.Id);

        Assert.AreEqual(RequestForQuotationStatus.Closed, closed.Status);
        Assert.AreEqual(SupplierQuotationStatus.Finalized, closed.SupplierQuotations.Single().Status);
        Assert.AreEqual("BRL", closed.SupplierQuotations.Single().CurrencyCode);
        Assert.AreEqual(25.123456m, closed.SupplierQuotations.Single().Lines.Single().UnitPrice);
        Assert.AreEqual(RequestForQuotationStatus.Closed, repository.Items.Single().Status);
    }

    [TestMethod]
    public async Task MutationRequiringActorRejectsMissingOrInactiveSession()
    {
        var source = SubmittedPurchaseRequest();
        var purchaseRequests = new InMemoryPurchaseRequestRepository();
        await purchaseRequests.AddAsync(source);

        var withoutSession = NewService(
            purchaseRequests: purchaseRequests,
            session: new TestSession(null));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => withoutSession.CreateDraftAsync(source.Id));

        var inactive = new UserResult(Guid.NewGuid(), "inactive", false);
        var inactiveSession = NewService(
            purchaseRequests: purchaseRequests,
            session: new TestSession(inactive));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => inactiveSession.CreateDraftAsync(source.Id));
    }

    private static PurchaseRequest SubmittedPurchaseRequest()
    {
        var request = new PurchaseRequest(Guid.NewGuid(), Now);
        request.AddLine("Item técnico", 2m, Guid.NewGuid());
        request.Submit(Guid.NewGuid(), Now);
        return request;
    }

    private static RequestForQuotationService NewService(
        InMemoryRequestForQuotationRepository? repository = null,
        InMemoryPurchaseRequestRepository? purchaseRequests = null,
        InMemorySupplierRepository? suppliers = null,
        RecordingAuthorizationService? authorization = null,
        TestSession? session = null,
        TestClock? clock = null)
    {
        return new RequestForQuotationService(
            repository ?? new InMemoryRequestForQuotationRepository(),
            purchaseRequests ?? new InMemoryPurchaseRequestRepository(),
            suppliers ?? new InMemorySupplierRepository(),
            authorization ?? new RecordingAuthorizationService(),
            session ?? new TestSession(new UserResult(Guid.NewGuid(), "buyer", true)),
            clock ?? new TestClock(Now));
    }

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

    private sealed class InMemoryRequestForQuotationRepository : IRequestForQuotationRepository
    {
        public List<RequestForQuotation> Items { get; } = [];

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
                .OrderByDescending(item => item.CreatedAtUtc)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(RequestForQuotation requestForQuotation, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryPurchaseRequestRepository : IPurchaseRequestRepository
    {
        public List<PurchaseRequest> Items { get; } = [];

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

    private sealed class InMemorySupplierRepository : ISupplierRepository
    {
        public List<Supplier> Items { get; } = [];

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

        public Task<IReadOnlyList<Supplier>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default)
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
}
