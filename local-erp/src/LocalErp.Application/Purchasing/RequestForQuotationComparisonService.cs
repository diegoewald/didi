using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Security;

namespace LocalErp.Application.Purchasing;

public sealed record RequestForQuotationComparisonSupplierResult(
    Guid SupplierId,
    string SupplierName,
    bool SupplierIsActive,
    SupplierQuotationStatus? ResponseStatus,
    string? CurrencyCode,
    string? SupplierReference,
    DateOnly? ValidUntilDate,
    DateTime? FinalizedAtUtc);

public sealed record RequestForQuotationComparisonOfferResult(
    Guid SupplierId,
    decimal? UnitPrice,
    DateOnly? PromisedDeliveryDate,
    string? Notes);

public sealed record RequestForQuotationComparisonLineResult(
    Guid RequestForQuotationLineId,
    int LineNumber,
    Guid PurchaseRequestLineId,
    string Description,
    decimal Quantity,
    Guid MeasurementUnitId,
    string MeasurementUnitName,
    DateOnly? NeededByDate,
    IReadOnlyList<RequestForQuotationComparisonOfferResult> Offers);

public sealed record RequestForQuotationComparisonResult(
    Guid RequestForQuotationId,
    Guid PurchaseRequestId,
    RequestForQuotationStatus Status,
    IReadOnlyList<RequestForQuotationComparisonSupplierResult> Suppliers,
    IReadOnlyList<RequestForQuotationComparisonLineResult> Lines);

public sealed class RequestForQuotationComparisonService(
    IRequestForQuotationRepository requestForQuotationRepository,
    IPurchaseRequestRepository purchaseRequestRepository,
    IMeasurementUnitRepository measurementUnitRepository,
    ISupplierRepository supplierRepository,
    IAuthorizationService authorizationService)
{
    private readonly IRequestForQuotationRepository _requestForQuotationRepository = requestForQuotationRepository
        ?? throw new ArgumentNullException(nameof(requestForQuotationRepository));
    private readonly IPurchaseRequestRepository _purchaseRequestRepository = purchaseRequestRepository
        ?? throw new ArgumentNullException(nameof(purchaseRequestRepository));
    private readonly IMeasurementUnitRepository _measurementUnitRepository = measurementUnitRepository
        ?? throw new ArgumentNullException(nameof(measurementUnitRepository));
    private readonly ISupplierRepository _supplierRepository = supplierRepository
        ?? throw new ArgumentNullException(nameof(supplierRepository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));

    public async Task<RequestForQuotationComparisonResult> GetComparisonAsync(
        Guid requestForQuotationId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsRead, cancellationToken);
        if (requestForQuotationId == Guid.Empty)
        {
            throw new ArgumentException("RFQ identifier cannot be empty.", nameof(requestForQuotationId));
        }

        var requestForQuotation = await _requestForQuotationRepository.GetByIdAsync(
                requestForQuotationId,
                cancellationToken)
            ?? throw new KeyNotFoundException($"RFQ '{requestForQuotationId}' was not found.");

        var purchaseRequest = await _purchaseRequestRepository.GetByIdAsync(
                requestForQuotation.PurchaseRequestId,
                cancellationToken)
            ?? throw new KeyNotFoundException(
                $"Purchase request '{requestForQuotation.PurchaseRequestId}' referenced by RFQ '{requestForQuotation.Id}' was not found.");

        var unitNames = (await _measurementUnitRepository.ListAsync(includeInactive: true, cancellationToken))
            .ToDictionary(unit => unit.Id, unit => unit.Name);
        var sourceLines = purchaseRequest.Lines.ToDictionary(line => line.Id);

        var suppliers = new List<RequestForQuotationComparisonSupplierResult>();
        foreach (var invitedSupplier in requestForQuotation.Suppliers)
        {
            var supplier = await _supplierRepository.GetByIdAsync(invitedSupplier.SupplierId, cancellationToken)
                ?? throw new KeyNotFoundException(
                    $"Supplier '{invitedSupplier.SupplierId}' referenced by RFQ '{requestForQuotation.Id}' was not found.");

            var quotation = requestForQuotation.SupplierQuotations
                .SingleOrDefault(item => item.SupplierId == invitedSupplier.SupplierId);
            var finalizedQuotation = quotation?.Status == SupplierQuotationStatus.Finalized
                ? quotation
                : null;

            suppliers.Add(new RequestForQuotationComparisonSupplierResult(
                supplier.Id,
                supplier.Name,
                supplier.IsActive,
                quotation?.Status,
                finalizedQuotation?.CurrencyCode,
                finalizedQuotation?.SupplierReference,
                finalizedQuotation?.ValidUntilDate,
                finalizedQuotation?.FinalizedAtUtc));
        }

        var orderedSuppliers = suppliers
            .OrderBy(supplier => supplier.SupplierName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(supplier => supplier.SupplierId)
            .ToArray();
        var finalizedBySupplier = requestForQuotation.SupplierQuotations
            .Where(quotation => quotation.Status == SupplierQuotationStatus.Finalized)
            .ToDictionary(quotation => quotation.SupplierId);

        var lines = new List<RequestForQuotationComparisonLineResult>();
        foreach (var rfqLine in requestForQuotation.Lines.OrderBy(line => line.LineNumber))
        {
            if (!sourceLines.TryGetValue(rfqLine.PurchaseRequestLineId, out var sourceLine))
            {
                throw new KeyNotFoundException(
                    $"Purchase request line '{rfqLine.PurchaseRequestLineId}' referenced by RFQ line '{rfqLine.Id}' was not found in the source request.");
            }

            if (!unitNames.TryGetValue(sourceLine.MeasurementUnitId, out var measurementUnitName))
            {
                throw new KeyNotFoundException(
                    $"Measurement unit '{sourceLine.MeasurementUnitId}' referenced by purchase request line '{sourceLine.Id}' was not found.");
            }

            var offers = orderedSuppliers
                .Select(supplier => MapOffer(supplier.SupplierId, rfqLine.Id, finalizedBySupplier))
                .ToArray();

            lines.Add(new RequestForQuotationComparisonLineResult(
                rfqLine.Id,
                rfqLine.LineNumber,
                sourceLine.Id,
                sourceLine.Description,
                sourceLine.Quantity,
                sourceLine.MeasurementUnitId,
                measurementUnitName,
                sourceLine.NeededByDate,
                offers));
        }

        return new RequestForQuotationComparisonResult(
            requestForQuotation.Id,
            requestForQuotation.PurchaseRequestId,
            requestForQuotation.Status,
            orderedSuppliers,
            lines);
    }

    private static RequestForQuotationComparisonOfferResult MapOffer(
        Guid supplierId,
        Guid requestForQuotationLineId,
        IReadOnlyDictionary<Guid, SupplierQuotation> finalizedBySupplier)
    {
        if (!finalizedBySupplier.TryGetValue(supplierId, out var quotation))
        {
            return new RequestForQuotationComparisonOfferResult(supplierId, null, null, null);
        }

        var line = quotation.Lines.SingleOrDefault(item => item.RequestForQuotationLineId == requestForQuotationLineId);
        return line is null
            ? new RequestForQuotationComparisonOfferResult(supplierId, null, null, null)
            : new RequestForQuotationComparisonOfferResult(
                supplierId,
                line.UnitPrice,
                line.PromisedDeliveryDate,
                line.Notes);
    }
}
