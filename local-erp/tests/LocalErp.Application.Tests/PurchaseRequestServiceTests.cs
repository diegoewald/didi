using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Purchasing;
using LocalErp.Application.Security;
using LocalErp.Application.Users;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Security;
using LocalErp.Domain.Units;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class PurchaseRequestServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 12, 30, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task CreateDraftUsesAuthenticatedUserClockAndManagePermission()
    {
        var repository = new InMemoryPurchaseRequestRepository();
        var authorization = new RecordingAuthorizationService();
        var user = new UserResult(Guid.NewGuid(), "buyer", true);
        var service = NewService(repository, authorization, new TestSession(user), new TestClock(Now));

        var result = await service.CreateDraftAsync("  Reposição  ");

        Assert.AreEqual(PurchaseRequestStatus.Draft, result.Status);
        Assert.AreEqual(user.Id, result.RequesterUserId);
        Assert.AreEqual(Now, result.CreatedAtUtc);
        Assert.AreEqual("Reposição", result.Notes);
        Assert.AreEqual(1, repository.Items.Count);
        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.PurchaseRequestsManage },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task ListAndGetDemandReadPermission()
    {
        var repository = new InMemoryPurchaseRequestRepository();
        var request = new PurchaseRequest(Guid.NewGuid(), Now);
        await repository.AddAsync(request);
        var authorization = new RecordingAuthorizationService();
        var service = NewService(repository, authorization);

        await service.ListAsync();
        await service.GetByIdAsync(request.Id);

        CollectionAssert.AreEqual(
            new[]
            {
                PermissionCatalog.PurchaseRequestsRead,
                PermissionCatalog.PurchaseRequestsRead,
            },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task ListActiveMeasurementUnitsUsesPurchaseManagePermissionAndFiltersInactive()
    {
        var repository = new InMemoryPurchaseRequestRepository();
        var units = new InMemoryMeasurementUnitRepository();
        var active = new MeasurementUnit("Peça");
        var inactive = new MeasurementUnit("Caixa");
        inactive.Deactivate();
        await units.AddAsync(active);
        await units.AddAsync(inactive);
        var authorization = new RecordingAuthorizationService();
        var service = NewService(repository, authorization, units: units);

        var result = await service.ListActiveMeasurementUnitsAsync();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(active.Id, result[0].Id);
        Assert.AreEqual("Peça", result[0].Name);
        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.PurchaseRequestsManage },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task AddLineRequiresExistingActiveMeasurementUnit()
    {
        var repository = new InMemoryPurchaseRequestRepository();
        var request = new PurchaseRequest(Guid.NewGuid(), Now);
        await repository.AddAsync(request);
        var units = new InMemoryMeasurementUnitRepository();
        var inactive = new MeasurementUnit("Unidade");
        inactive.Deactivate();
        await units.AddAsync(inactive);
        var service = NewService(repository, units: units);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.AddLineAsync(
            request.Id,
            "Item",
            1m,
            inactive.Id));

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(() => service.AddLineAsync(
            request.Id,
            "Item",
            1m,
            Guid.NewGuid()));
    }

    [TestMethod]
    public async Task FullDraftLifecyclePersistsDomainTransitions()
    {
        var repository = new InMemoryPurchaseRequestRepository();
        var units = new InMemoryMeasurementUnitRepository();
        var unit = new MeasurementUnit("Peça");
        await units.AddAsync(unit);
        var user = new UserResult(Guid.NewGuid(), "buyer", true);
        var service = NewService(
            repository,
            session: new TestSession(user),
            clock: new TestClock(Now),
            units: units);

        var created = await service.CreateDraftAsync();
        var withLine = await service.AddLineAsync(created.Id, "  Item técnico  ", 3m, unit.Id);
        var submitted = await service.SubmitAsync(created.Id);

        Assert.AreEqual(1, withLine.Lines.Count);
        Assert.AreEqual("Item técnico", withLine.Lines[0].Description);
        Assert.AreEqual(PurchaseRequestStatus.Submitted, submitted.Status);
        Assert.AreEqual(user.Id, submitted.SubmittedByUserId);
        Assert.AreEqual(Now, submitted.SubmittedAtUtc);
        Assert.AreEqual(PurchaseRequestStatus.Submitted, repository.Items.Single().Status);
    }

    [TestMethod]
    public async Task CancelRecordsCurrentActorAndReason()
    {
        var repository = new InMemoryPurchaseRequestRepository();
        var request = new PurchaseRequest(Guid.NewGuid(), Now);
        await repository.AddAsync(request);
        var user = new UserResult(Guid.NewGuid(), "buyer", true);
        var service = NewService(repository, session: new TestSession(user), clock: new TestClock(Now));

        var result = await service.CancelAsync(request.Id, "  Demanda retirada  ");

        Assert.AreEqual(PurchaseRequestStatus.Cancelled, result.Status);
        Assert.AreEqual(user.Id, result.CancelledByUserId);
        Assert.AreEqual("Demanda retirada", result.CancellationReason);
    }

    [TestMethod]
    public async Task MutationRequiresAuthenticatedActiveUserWhenActorIsNeeded()
    {
        var repository = new InMemoryPurchaseRequestRepository();
        var serviceWithoutSession = NewService(repository, session: new TestSession(null));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => serviceWithoutSession.CreateDraftAsync());

        var inactiveUser = new UserResult(Guid.NewGuid(), "inactive", false);
        var serviceWithInactiveUser = NewService(repository, session: new TestSession(inactiveUser));

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => serviceWithInactiveUser.CreateDraftAsync());
    }

    private static PurchaseRequestService NewService(
        InMemoryPurchaseRequestRepository repository,
        RecordingAuthorizationService? authorization = null,
        TestSession? session = null,
        TestClock? clock = null,
        InMemoryMeasurementUnitRepository? units = null)
    {
        return new PurchaseRequestService(
            repository,
            units ?? new InMemoryMeasurementUnitRepository(),
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
                .OrderByDescending(item => item.CreatedAtUtc)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(PurchaseRequest purchaseRequest, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryMeasurementUnitRepository : IMeasurementUnitRepository
    {
        public List<MeasurementUnit> Items { get; } = [];

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
