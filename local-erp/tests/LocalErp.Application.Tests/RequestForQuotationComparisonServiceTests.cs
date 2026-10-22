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
public sealed class RequestForQuotationComparisonServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 15, 20, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task ComparisonUsesReadPermissionAndIgnoresDraftPrices()
    {
        var fixture = await Fixture.CreateAsync();
        fixture.SecondSupplier.Deactivate();

        var result = await fixture.Service.GetComparisonAsync(fixture.RequestForQuotation.Id);

        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.RequestForQuotationsRead },
            fixture.Authorization.Demands.ToArray());
        Assert.AreEqual(RequestForQuotationStatus.Sent, result.Status);
        Assert.AreEqual(2, result.Suppliers.Count);

        var finalized = result.Suppliers.Single(item => item.SupplierId == fixture.FirstSupplier.Id);
        Assert.AreEqual(SupplierQuotationStatus.Finalized, finalized.ResponseStatus);
        Assert.AreEqual("BRL", finalized.CurrencyCode);
        Assert.AreEqual("A-001", finalized.SupplierReference);

        var draft = result.Suppliers.Single(item => item.SupplierId == fixture.SecondSupplier.Id);
        Assert.AreEqual(SupplierQuotationStatus.Draft, draft.ResponseStatus);
        Assert.IsFalse(draft.SupplierIsActive);
        Assert.IsNull(draft.CurrencyCode);

        var line = result.Lines.Single();
        Assert.AreEqual("Peça histórica", line.MeasurementUnitName);
        Assert.AreEqual("Item técnico", line.Description);
        var finalizedOffer = line.Offers.Single(item => item.SupplierId == fixture.FirstSupplier.Id);
        Assert.IsNotNull(finalizedOffer.SupplierQuotationLineId);
        Assert.AreEqual(25.123456m, finalizedOffer.UnitPrice);
        Assert.AreEqual(new DateOnly(2026, 11, 10), finalizedOffer.PromisedDeliveryDate);
        var draftOffer = line.Offers.Single(item => item.SupplierId == fixture.SecondSupplier.Id);
        Assert.IsNull(draftOffer.SupplierQuotationLineId);
        Assert.IsNull(draftOffer.UnitPrice);
    }

    [TestMethod]
    public async Task ComparisonPreservesDifferentCurrenciesAndZeroPriceWithoutRanking()
    {
        var fixture = await Fixture.CreateAsync(finalizeSecondQuotation: true, secondPrice: 0m);

        var result = await fixture.Service.GetComparisonAsync(fixture.RequestForQuotation.Id);

        Assert.AreEqual("BRL", result.Suppliers.Single(item => item.SupplierId == fixture.FirstSupplier.Id).CurrencyCode);
        Assert.AreEqual("USD", result.Suppliers.Single(item => item.SupplierId == fixture.SecondSupplier.Id).CurrencyCode);
        var line = result.Lines.Single();
        var firstOffer = line.Offers.Single(item => item.SupplierId == fixture.FirstSupplier.Id);
        var secondOffer = line.Offers.Single(item => item.SupplierId == fixture.SecondSupplier.Id);
        Assert.IsNotNull(firstOffer.SupplierQuotationLineId);
        Assert.IsNotNull(secondOffer.SupplierQuotationLineId);
        Assert.AreEqual(25.123456m, firstOffer.UnitPrice);
        Assert.AreEqual(0m, secondOffer.UnitPrice);
    }

    [TestMethod]
    public async Task ComparisonFailsExplicitlyWhenHistoricalUnitIsMissing()
    {
        var fixture = await Fixture.CreateAsync();
        fixture.Units.Items.Clear();

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
            () => fixture.Service.GetComparisonAsync(fixture.RequestForQuotation.Id));
    }

    private sealed class Fixture
    {
        private Fixture(
            RequestForQuotationComparisonService service,
            RecordingAuthorizationService authorization,
            MeasurementUnitRepository units,
            Supplier firstSupplier,
            Supplier secondSupplier,
            RequestForQuotation requestForQuotation)
        {
            Service = service;
            Authorization = authorization;
            Units = units;
            FirstSupplier = firstSupplier;
            SecondSupplier = secondSupplier;
            RequestForQuotation = requestForQuotation;
        }

        public RequestForQuotationComparisonService Service { get; }
        public RecordingAuthorizationService Authorization { get; }
        public MeasurementUnitRepository Units { get; }
        public Supplier FirstSupplier { get; }
        public Supplier SecondSupplier { get; }
        public RequestForQuotation RequestForQuotation { get; }

        public static async Task<Fixture> CreateAsync(
            bool finalizeSecondQuotation = false,
            decimal secondPrice = 9.5m)
        {
            var purchaseRequests = new PurchaseRequestRepository();
            var rfqs = new RequestForQuotationRepository();
            var units = new MeasurementUnitRepository();
            var suppliers = new SupplierRepository();
            var authorization = new RecordingAuthorizationService();

            var unit = new MeasurementUnit("Peça histórica");
            await units.AddAsync(unit);
            var request = new PurchaseRequest(Guid.NewGuid(), Now);
            request.AddLine("Item técnico", 2m, unit.Id, new DateOnly(2026, 11, 5));
            request.Submit(Guid.NewGuid(), Now.AddMinutes(1));
            await purchaseRequests.AddAsync(request);

            var firstSupplier = new Supplier("Fornecedor A");
            var secondSupplier = new Supplier("Fornecedor B");
            await suppliers.AddAsync(firstSupplier);
            await suppliers.AddAsync(secondSupplier);

            var actor = Guid.NewGuid();
            var rfq = new RequestForQuotation(request.Id, actor, Now.AddMinutes(2));
            var rfqLine = rfq.AddSourceLine(request.Lines.Single().Id);
            rfq.AddSupplier(firstSupplier.Id);
            rfq.AddSupplier(secondSupplier.Id);
            rfq.Send(actor, Now.AddMinutes(3));

            var firstQuotation = rfq.CreateSupplierQuotation(
                firstSupplier.Id,
                "brl",
                actor,
                Now.AddMinutes(4),
                "A-001",
                new DateOnly(2026, 12, 1));
            rfq.UpsertSupplierQuotationLine(
                firstQuotation.Id,
                rfqLine.Id,
                25.123456m,
                new DateOnly(2026, 11, 10),
                "Entrega padrão");
            rfq.FinalizeSupplierQuotation(firstQuotation.Id, actor, Now.AddMinutes(5));

            var secondQuotation = rfq.CreateSupplierQuotation(
                secondSupplier.Id,
                "usd",
                actor,
                Now.AddMinutes(6),
                "B-001");
            rfq.UpsertSupplierQuotationLine(
                secondQuotation.Id,
                rfqLine.Id,
                secondPrice,
                new DateOnly(2026, 11, 8));
            if (finalizeSecondQuotation)
            {
                rfq.FinalizeSupplierQuotation(secondQuotation.Id, actor, Now.AddMinutes(7));
            }

            await rfqs.AddAsync(rfq);
            var service = new RequestForQuotationComparisonService(
                rfqs,
                purchaseRequests,
                units,
                suppliers,
                authorization);

            return new Fixture(service, authorization, units, firstSupplier, secondSupplier, rfq);
        }
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

    private sealed class RequestForQuotationRepository : IRequestForQuotationRepository
    {
        private readonly List<RequestForQuotation> _items = [];

        public Task AddAsync(RequestForQuotation item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Add(item);
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

        public Task UpdateAsync(RequestForQuotation item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class PurchaseRequestRepository : IPurchaseRequestRepository
    {
        private readonly List<PurchaseRequest> _items = [];

        public Task AddAsync(PurchaseRequest item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Add(item);
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

        public Task UpdateAsync(PurchaseRequest item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class MeasurementUnitRepository : IMeasurementUnitRepository
    {
        public List<MeasurementUnit> Items { get; } = [];

        public Task AddAsync(MeasurementUnit item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Items.Add(item);
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

        public Task UpdateAsync(MeasurementUnit item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class SupplierRepository : ISupplierRepository
    {
        private readonly List<Supplier> _items = [];

        public Task AddAsync(Supplier item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            _items.Add(item);
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

        public Task UpdateAsync(Supplier item, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
