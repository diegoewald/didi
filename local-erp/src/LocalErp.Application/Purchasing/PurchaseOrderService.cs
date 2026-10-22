using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Security;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Security;
using LocalErp.Domain.Suppliers;
using LocalErp.Domain.Units;

namespace LocalErp.Application.Purchasing;

public sealed class PurchaseOrderService(
    IPurchaseOrderRepository repository,
    IRequestForQuotationAwardRepository awardRepository,
    IRequestForQuotationRepository requestForQuotationRepository,
    IPurchaseRequestRepository purchaseRequestRepository,
    ISupplierRepository supplierRepository,
    IMeasurementUnitRepository measurementUnitRepository,
    IAuthorizationService authorizationService,
    IUserSession session,
    IClock clock)
{
    private readonly IPurchaseOrderRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IRequestForQuotationAwardRepository _awardRepository = awardRepository
        ?? throw new ArgumentNullException(nameof(awardRepository));
    private readonly IRequestForQuotationRepository _requestForQuotationRepository = requestForQuotationRepository
        ?? throw new ArgumentNullException(nameof(requestForQuotationRepository));
    private readonly IPurchaseRequestRepository _purchaseRequestRepository = purchaseRequestRepository
        ?? throw new ArgumentNullException(nameof(purchaseRequestRepository));
    private readonly ISupplierRepository _supplierRepository = supplierRepository
        ?? throw new ArgumentNullException(nameof(supplierRepository));
    private readonly IMeasurementUnitRepository _measurementUnitRepository = measurementUnitRepository
        ?? throw new ArgumentNullException(nameof(measurementUnitRepository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));
    private readonly IUserSession _session = session
        ?? throw new ArgumentNullException(nameof(session));
    private readonly IClock _clock = clock
        ?? throw new ArgumentNullException(nameof(clock));

    public async Task<PurchaseOrderResult> CreateDraftFromAwardAsync(
        Guid requestForQuotationAwardId,
        Guid supplierId,
        string? notes = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseOrdersManage, cancellationToken);
        var actorUserId = GetCurrentUserId();
        var award = await GetRequiredAwardAsync(requestForQuotationAwardId, cancellationToken);
        if (award.Status != RequestForQuotationAwardStatus.Finalized)
        {
            throw new InvalidOperationException("Purchase orders can only be created from finalized RFQ awards.");
        }

        var supplier = await GetRequiredSupplierAsync(supplierId, cancellationToken);
        EnsureActiveSupplier(supplier, "Inactive suppliers cannot receive new purchase orders.");

        if (await _repository.GetByAwardAndSupplierAsync(award.Id, supplier.Id, cancellationToken) is not null)
        {
            throw new InvalidOperationException(
                "A purchase order already exists for this RFQ award and supplier.");
        }

        var requestForQuotation = await GetRequiredRequestForQuotationAsync(
            award.RequestForQuotationId,
            cancellationToken);
        if (requestForQuotation.Status != RequestForQuotationStatus.Closed)
        {
            throw new InvalidOperationException("A finalized award must reference a closed RFQ.");
        }

        var resolvedLines = ResolveAwardedLinesForSupplier(award, requestForQuotation, supplier.Id);
        if (resolvedLines.Count == 0)
        {
            throw new InvalidOperationException(
                "The selected supplier does not have awarded lines in this RFQ award.");
        }

        var purchaseRequest = await GetRequiredPurchaseRequestAsync(
            requestForQuotation.PurchaseRequestId,
            cancellationToken);
        var snapshots = new List<PurchaseOrderLineSnapshot>(resolvedLines.Count);
        foreach (var resolvedLine in resolvedLines.OrderBy(item => item.RequestForQuotationLine.LineNumber))
        {
            var purchaseRequestLine = purchaseRequest.Lines.SingleOrDefault(
                line => line.Id == resolvedLine.RequestForQuotationLine.PurchaseRequestLineId)
                ?? throw new InvalidOperationException(
                    "An awarded RFQ line references a purchase request line that no longer exists.");
            var measurementUnit = await GetRequiredMeasurementUnitAsync(
                purchaseRequestLine.MeasurementUnitId,
                cancellationToken);

            snapshots.Add(new PurchaseOrderLineSnapshot(
                resolvedLine.AwardLine.Id,
                purchaseRequestLine.Description,
                purchaseRequestLine.Quantity,
                measurementUnit.Id,
                measurementUnit.Name,
                resolvedLine.SupplierQuotationLine.UnitPrice,
                resolvedLine.SupplierQuotationLine.PromisedDeliveryDate));
        }

        var currencies = resolvedLines
            .Select(item => item.SupplierQuotation.CurrencyCode)
            .Distinct(StringComparer.Ordinal)
            .ToArray();
        if (currencies.Length != 1)
        {
            throw new InvalidOperationException(
                "All awarded lines in a purchase order must use the same currency.");
        }

        var purchaseOrder = await _repository.AddWithNextSequenceAsync(
            sequenceNumber => new PurchaseOrder(
                sequenceNumber,
                award.Id,
                supplier.Id,
                supplier.Name,
                currencies[0],
                actorUserId,
                _clock.UtcNow,
                snapshots,
                notes),
            cancellationToken);

        return PurchaseOrderResult.FromDomain(purchaseOrder);
    }

    public async Task<IReadOnlyList<PurchaseOrderResult>> ListAsync(
        PurchaseOrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseOrdersRead, cancellationToken);
        var purchaseOrders = await _repository.ListAsync(status, cancellationToken);
        return purchaseOrders
            .OrderByDescending(purchaseOrder => purchaseOrder.SequenceNumber)
            .Select(PurchaseOrderResult.FromDomain)
            .ToArray();
    }

    public async Task<PurchaseOrderResult> GetByIdAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseOrdersRead, cancellationToken);
        return PurchaseOrderResult.FromDomain(
            await GetRequiredPurchaseOrderAsync(purchaseOrderId, cancellationToken));
    }

    public async Task<PurchaseOrderResult> UpdateDraftAsync(
        Guid purchaseOrderId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseOrdersManage, cancellationToken);
        _ = GetCurrentUserId();
        var purchaseOrder = await GetRequiredPurchaseOrderAsync(purchaseOrderId, cancellationToken);
        purchaseOrder.UpdateNotes(notes);
        await _repository.UpdateAsync(purchaseOrder, cancellationToken);
        return PurchaseOrderResult.FromDomain(purchaseOrder);
    }

    public async Task<PurchaseOrderResult> IssueAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseOrdersManage, cancellationToken);
        var actorUserId = GetCurrentUserId();
        var purchaseOrder = await GetRequiredPurchaseOrderAsync(purchaseOrderId, cancellationToken);
        var supplier = await GetRequiredSupplierAsync(purchaseOrder.SupplierId, cancellationToken);
        EnsureActiveSupplier(supplier, "Inactive suppliers cannot receive newly issued purchase orders.");

        purchaseOrder.Issue(actorUserId, _clock.UtcNow);
        await _repository.UpdateAsync(purchaseOrder, cancellationToken);
        return PurchaseOrderResult.FromDomain(purchaseOrder);
    }

    public async Task<PurchaseOrderResult> CancelAsync(
        Guid purchaseOrderId,
        string reason,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseOrdersManage, cancellationToken);
        var actorUserId = GetCurrentUserId();
        var purchaseOrder = await GetRequiredPurchaseOrderAsync(purchaseOrderId, cancellationToken);
        purchaseOrder.Cancel(actorUserId, _clock.UtcNow, reason);
        await _repository.UpdateAsync(purchaseOrder, cancellationToken);
        return PurchaseOrderResult.FromDomain(purchaseOrder);
    }

    private async Task<PurchaseOrder> GetRequiredPurchaseOrderAsync(
        Guid purchaseOrderId,
        CancellationToken cancellationToken)
    {
        if (purchaseOrderId == Guid.Empty)
        {
            throw new ArgumentException("Purchase order identifier cannot be empty.", nameof(purchaseOrderId));
        }

        return await _repository.GetByIdAsync(purchaseOrderId, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase order '{purchaseOrderId}' was not found.");
    }

    private async Task<RequestForQuotationAward> GetRequiredAwardAsync(
        Guid requestForQuotationAwardId,
        CancellationToken cancellationToken)
    {
        if (requestForQuotationAwardId == Guid.Empty)
        {
            throw new ArgumentException(
                "RFQ award identifier cannot be empty.",
                nameof(requestForQuotationAwardId));
        }

        return await _awardRepository.GetByIdAsync(requestForQuotationAwardId, cancellationToken)
            ?? throw new KeyNotFoundException(
                $"RFQ award decision '{requestForQuotationAwardId}' was not found.");
    }

    private async Task<RequestForQuotation> GetRequiredRequestForQuotationAsync(
        Guid requestForQuotationId,
        CancellationToken cancellationToken)
    {
        return await _requestForQuotationRepository.GetByIdAsync(requestForQuotationId, cancellationToken)
            ?? throw new KeyNotFoundException($"RFQ '{requestForQuotationId}' was not found.");
    }

    private async Task<PurchaseRequest> GetRequiredPurchaseRequestAsync(
        Guid purchaseRequestId,
        CancellationToken cancellationToken)
    {
        return await _purchaseRequestRepository.GetByIdAsync(purchaseRequestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase request '{purchaseRequestId}' was not found.");
    }

    private async Task<Supplier> GetRequiredSupplierAsync(
        Guid supplierId,
        CancellationToken cancellationToken)
    {
        if (supplierId == Guid.Empty)
        {
            throw new ArgumentException("Supplier identifier cannot be empty.", nameof(supplierId));
        }

        return await _supplierRepository.GetByIdAsync(supplierId, cancellationToken)
            ?? throw new KeyNotFoundException($"Supplier '{supplierId}' was not found.");
    }

    private async Task<MeasurementUnit> GetRequiredMeasurementUnitAsync(
        Guid measurementUnitId,
        CancellationToken cancellationToken)
    {
        return await _measurementUnitRepository.GetByIdAsync(measurementUnitId, cancellationToken)
            ?? throw new KeyNotFoundException($"Measurement unit '{measurementUnitId}' was not found.");
    }

    private static List<ResolvedAwardLine> ResolveAwardedLinesForSupplier(
        RequestForQuotationAward award,
        RequestForQuotation requestForQuotation,
        Guid supplierId)
    {
        var result = new List<ResolvedAwardLine>();
        foreach (var awardLine in award.Lines)
        {
            var requestForQuotationLine = requestForQuotation.Lines.SingleOrDefault(
                line => line.Id == awardLine.RequestForQuotationLineId)
                ?? throw new InvalidOperationException(
                    "An RFQ award references an RFQ line that does not exist.");
            var selected = FindSupplierQuotationLine(
                requestForQuotation,
                awardLine.SupplierQuotationLineId);

            if (selected.Quotation.Status != SupplierQuotationStatus.Finalized)
            {
                throw new InvalidOperationException(
                    "Purchase orders can only use finalized supplier quotation lines.");
            }

            if (selected.Line.RequestForQuotationLineId != requestForQuotationLine.Id)
            {
                throw new InvalidOperationException(
                    "An awarded supplier quotation line does not match its RFQ line.");
            }

            if (selected.Quotation.SupplierId != supplierId)
            {
                continue;
            }

            result.Add(new ResolvedAwardLine(
                awardLine,
                requestForQuotationLine,
                selected.Quotation,
                selected.Line));
        }

        return result;
    }

    private static (SupplierQuotation Quotation, SupplierQuotationLine Line) FindSupplierQuotationLine(
        RequestForQuotation requestForQuotation,
        Guid supplierQuotationLineId)
    {
        foreach (var quotation in requestForQuotation.SupplierQuotations)
        {
            var line = quotation.Lines.SingleOrDefault(item => item.Id == supplierQuotationLineId);
            if (line is not null)
            {
                return (quotation, line);
            }
        }

        throw new InvalidOperationException(
            "An RFQ award references a supplier quotation line that does not exist.");
    }

    private static void EnsureActiveSupplier(Supplier supplier, string message)
    {
        if (!supplier.IsActive)
        {
            throw new InvalidOperationException(message);
        }
    }

    private Guid GetCurrentUserId()
    {
        var currentUser = _session.CurrentUser
            ?? throw new InvalidOperationException("Purchase order mutations require an authenticated session.");
        if (!currentUser.IsActive)
        {
            throw new InvalidOperationException("The authenticated user is not active.");
        }

        return currentUser.Id;
    }

    private sealed record ResolvedAwardLine(
        RequestForQuotationAwardLine AwardLine,
        RequestForQuotationLine RequestForQuotationLine,
        SupplierQuotation SupplierQuotation,
        SupplierQuotationLine SupplierQuotationLine);
}
