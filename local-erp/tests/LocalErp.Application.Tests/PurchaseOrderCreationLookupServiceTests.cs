using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Purchasing;
using LocalErp.Application.Security;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Security;
using LocalErp.Domain.Suppliers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class PurchaseOrderCreationLookupServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 23, 10, 30, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task AvailableCandidatesAreGroupedBySupplierAndExcludeConvertedOrInactiveSuppliers()
    {
        var supplierA = new Supplier("Fornecedor A");
        var supplierB = new Supplier("Fornecedor B");
        var requestForQuotation = new RequestForQuotation(Guid.NewGuid(), Guid.NewGuid(), Now);
        var firstLine = requestForQuotation.AddSourceLine(Guid.NewGuid());
        var secondLine = requestForQuotation.AddSourceLine(Guid.NewGuid());
        var thirdLine = requestForQuotation.AddSourceLine(Guid.NewGuid());
        requestForQuotation.AddSupplier(supplierA.Id);
        requestForQuotation.AddSupplier(supplierB.Id);
        requestForQuotation.Send(Guid.NewGuid(), Now.AddMinutes(1));

        var quotationA = requestForQuotation.CreateSupplierQuotation(
            supplierA.Id,
            "BRL",
            Guid.NewGuid(),
            Now.AddMinutes(2));
        var quotationALine1 = requestForQuotation.UpsertSupplierQuotationLine(
            quotationA.Id,
            firstLine.Id,
            10m,
            new DateOnly(2026, 11, 5));
        var quotationALine2 = requestForQuotation.UpsertSupplierQuotationLine(
            quotationA.Id,
            secondLine.Id,
            20m,
            new DateOnly(2026, 11, 6));
        requestForQuotation.FinalizeSupplierQuotation(quotationA.Id, Guid.NewGuid(), Now.AddMinutes(3));

        var quotationB = requestForQuotation.CreateSupplierQuotation(
            supplierB.Id,
            "USD",
            Guid.NewGuid(),
            Now.AddMinutes(4));
        var quotationBLine = requestForQuotation.UpsertSupplierQuotationLine(
            quotationB.Id,
            thirdLine.Id,
            30m,
            new DateOnly(2026, 11, 7));
        requestForQuotation.FinalizeSupplierQuotation(quotationB.Id, Guid.NewGuid(), Now.AddMinutes(5));
        requestForQuotation.Close(Guid.NewGuid(), Now.AddMinutes(6));

        var award = new RequestForQuotationAward(requestForQuotation.Id, Guid.NewGuid(), Now.AddMinutes(7));
        award.SelectLine(firstLine.Id, quotationALine1.Id);
        award.SelectLine(secondLine.Id, quotationALine2.Id);
        award.SelectLine(thirdLine.Id, quotationBLine.Id);
        award.Finalize(Guid.NewGuid(), Now.AddMinutes(8));

        var orders = new PurchaseOrderRepository();
        var authorization = new RecordingAuthorizationService();
        var service = new PurchaseOrderCreationLookupService(
            orders,
            new AwardRepository([award]),
            new RequestForQuotationRepository([requestForQuotation]),
            new SupplierRepository([supplierA, supplierB]),
            authorization);

        var available = await service.ListAvailableAsync();

        Assert.AreEqual(2, available.Count);
        Assert.AreEqual(supplierA.Id, available[0].SupplierId);
        Assert.AreEqual("Fornecedor A", available[0].SupplierName);
        Assert.AreEqual(2, available[0].AwardedLineCount);
        Assert.AreEqual(award.Id, available[0].RequestForQuotationAwardId);
        Assert.AreEqual(requestForQuotation.Id, available[0].RequestForQuotationId);
        Assert.AreEqual(Now.AddMinutes(8), available[0].AwardFinalizedAtUtc);
        Assert.AreEqual(supplierB.Id, available[1].SupplierId);
        Assert.AreEqual(1, available[1].AwardedLineCount);
        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.PurchaseOrdersManage },
            authorization.Demands.ToArray());

        await orders.AddWithNextSequenceAsync(sequenceNumber =>
            new PurchaseOrder(
                sequenceNumber,
                award.Id,
                supplierA.Id,
                supplierA.Name,
                "BRL",
                Guid.NewGuid(),
                Now.AddMinutes(9),
                [
                    new PurchaseOrderLineSnapshot(
                        award.Lines[0].Id,
                        "Item convertido",
                        1m,
                        Guid.NewGuid(),
                        "UN",
                        1m,
                        null),
                ]));
        supplierB.Deactivate();
        authorization.Demands.Clear();

        available = await service.ListAvailableAsync();

        Assert.AreEqual(0, available.Count);
        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.PurchaseOrdersManage },
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

    private sealed class PurchaseOrderRepository : IPurchaseOrderRepository
    {
        private int _nextSequenceNumber = 1;
        private readonly List<PurchaseOrder> _items = [];

        public Task<PurchaseOrder> AddWithNextSequenceAsync(
            Func<int, PurchaseOrder> purchaseOrderFactory,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            ArgumentNullException.ThrowIfNull(purchaseOrderFactory);
            var item = purchaseOrderFactory(_nextSequenceNumber++);
            _items.Add(item);
            return Task.FromResult(item);
        }

        public Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_items.SingleOrDefault(item => item.Id == id));
        }

        public Task<PurchaseOrder?> GetByAwardAndSupplierAsync(
            Guid requestForQuotationAwardId,
            Guid supplierId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_items.SingleOrDefault(item =>
                item.RequestForQuotationAwardId == requestForQuotationAwardId &&
                item.SupplierId == supplierId));
        }

        public Task<IReadOnlyList<PurchaseOrder>> ListAsync(
            PurchaseOrderStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<PurchaseOrder> result = _items
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

    private sealed class AwardRepository(IEnumerable<RequestForQuotationAward> items)
        : IRequestForQuotationAwardRepository
    {
        private readonly List<RequestForQuotationAward> _items = [.. items];

        public Task AddAsync(RequestForQuotationAward award, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Add(award);
            return Task.CompletedTask;
        }

        public Task<RequestForQuotationAward?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_items.SingleOrDefault(item => item.Id == id));
        }

        public Task<RequestForQuotationAward?> GetByRequestForQuotationIdAsync(
            Guid requestForQuotationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_items.SingleOrDefault(item => item.RequestForQuotationId == requestForQuotationId));
        }

        public Task UpdateAsync(RequestForQuotationAward award, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class RequestForQuotationRepository(IEnumerable<RequestForQuotation> items)
        : IRequestForQuotationRepository
    {
        private readonly List<RequestForQuotation> _items = [.. items];

        public Task AddAsync(RequestForQuotation requestForQuotation, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Add(requestForQuotation);
            return Task.CompletedTask;
        }

        public Task<RequestForQuotation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(_items.SingleOrDefault(item => item.Id == id));
        }

        public Task<IReadOnlyList<RequestForQuotation>> ListAsync(
            RequestForQuotationStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<RequestForQuotation> result = _items
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

    private sealed class SupplierRepository(IEnumerable<Supplier> items) : ISupplierRepository
    {
        private readonly List<Supplier> _items = [.. items];

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

        public Task<IReadOnlyList<Supplier>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
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
