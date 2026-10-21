using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Purchasing;
using LocalErp.Application.Security;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Security;
using LocalErp.Domain.Suppliers;
using LocalErp.Domain.Units;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class RequestForQuotationLookupServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 13, 45, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task SubmittedSourcesUseRfqManagePermissionAndIncludeHistoricalUnitName()
    {
        var requests = new PurchaseRequestRepository();
        var units = new MeasurementUnitRepository();
        var unit = new MeasurementUnit("Peça");
        unit.Deactivate();
        await units.AddAsync(unit);
        var draft = new PurchaseRequest(Guid.NewGuid(), Now);
        draft.AddLine("Rascunho", 1m, unit.Id);
        await requests.AddAsync(draft);
        var submitted = new PurchaseRequest(Guid.NewGuid(), Now.AddMinutes(1));
        submitted.AddLine("Item", 2m, unit.Id);
        submitted.Submit(Guid.NewGuid(), Now.AddMinutes(2));
        await requests.AddAsync(submitted);
        var authorization = new RecordingAuthorizationService();
        var service = new RequestForQuotationLookupService(requests, units, new SupplierRepository(), authorization);

        var result = await service.ListSubmittedPurchaseRequestsAsync();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual(submitted.Id, result[0].Id);
        Assert.AreEqual("Peça", result[0].Lines.Single().MeasurementUnitName);
        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.RequestForQuotationsManage },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task HistoricalSourceCanBeReadAfterPurchaseRequestCancellation()
    {
        var requests = new PurchaseRequestRepository();
        var units = new MeasurementUnitRepository();
        var unit = new MeasurementUnit("Unidade");
        await units.AddAsync(unit);
        var request = new PurchaseRequest(Guid.NewGuid(), Now);
        request.AddLine("Item", 1m, unit.Id);
        request.Submit(Guid.NewGuid(), Now.AddMinutes(1));
        request.Cancel(Guid.NewGuid(), Now.AddMinutes(2), "Retirada");
        await requests.AddAsync(request);
        var authorization = new RecordingAuthorizationService();
        var service = new RequestForQuotationLookupService(requests, units, new SupplierRepository(), authorization);

        var result = await service.GetPurchaseRequestAsync(request.Id);

        Assert.AreEqual(PurchaseRequestStatus.Cancelled, result.Status);
        Assert.AreEqual("Item", result.Lines.Single().Description);
        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.RequestForQuotationsRead },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task ActiveSupplierSelectionAndHistoricalReferencesUseRfqPermissions()
    {
        var suppliers = new SupplierRepository();
        var active = new Supplier("Ativo");
        var inactive = new Supplier("Inativo");
        inactive.Deactivate();
        await suppliers.AddAsync(active);
        await suppliers.AddAsync(inactive);
        var authorization = new RecordingAuthorizationService();
        var service = new RequestForQuotationLookupService(
            new PurchaseRequestRepository(),
            new MeasurementUnitRepository(),
            suppliers,
            authorization);

        var activeOptions = await service.ListActiveSuppliersAsync();
        var historical = await service.GetSupplierReferencesAsync(new[] { inactive.Id, active.Id });

        Assert.AreEqual(1, activeOptions.Count);
        Assert.AreEqual(active.Id, activeOptions[0].Id);
        Assert.AreEqual(2, historical.Count);
        Assert.IsTrue(historical.Any(option => option.Id == inactive.Id && !option.IsActive));
        CollectionAssert.AreEqual(
            new[]
            {
                PermissionCatalog.RequestForQuotationsManage,
                PermissionCatalog.RequestForQuotationsRead,
            },
            authorization.Demands.ToArray());
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

    private sealed class PurchaseRequestRepository : IPurchaseRequestRepository
    {
        private readonly List<PurchaseRequest> _items = [];

        public Task AddAsync(PurchaseRequest purchaseRequest, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Add(purchaseRequest);
            return Task.CompletedTask;
        }

        public Task<PurchaseRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_items.SingleOrDefault(item => item.Id == id));
        }

        public Task<IReadOnlyList<PurchaseRequest>> ListAsync(
            PurchaseRequestStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<PurchaseRequest> result = _items
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

    private sealed class MeasurementUnitRepository : IMeasurementUnitRepository
    {
        private readonly List<MeasurementUnit> _items = [];

        public Task AddAsync(MeasurementUnit unit, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Add(unit);
            return Task.CompletedTask;
        }

        public Task<MeasurementUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_items.SingleOrDefault(item => item.Id == id));
        }

        public Task<IReadOnlyList<MeasurementUnit>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<MeasurementUnit> result = _items
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

    private sealed class SupplierRepository : ISupplierRepository
    {
        private readonly List<Supplier> _items = [];

        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Add(supplier);
            return Task.CompletedTask;
        }

        public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_items.SingleOrDefault(item => item.Id == id));
        }

        public Task<IReadOnlyList<Supplier>> ListAsync(bool includeInactive, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<Supplier> result = _items
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
