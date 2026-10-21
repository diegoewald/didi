using LocalErp.Domain.Purchasing;

namespace LocalErp.Application.Purchasing;

public sealed record RequestForQuotationLineResult(
    Guid Id,
    int LineNumber,
    Guid PurchaseRequestLineId);

public sealed record RequestForQuotationSupplierResult(Guid Id, Guid SupplierId);

public sealed record SupplierQuotationLineResult(
    Guid Id,
    Guid RequestForQuotationLineId,
    decimal UnitPrice,
    DateOnly? PromisedDeliveryDate,
    string? Notes);

public sealed record SupplierQuotationResult(
    Guid Id,
    Guid SupplierId,
    SupplierQuotationStatus Status,
    string CurrencyCode,
    string? SupplierReference,
    DateOnly? ValidUntilDate,
    string? Notes,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc,
    Guid? FinalizedByUserId,
    DateTime? FinalizedAtUtc,
    IReadOnlyList<SupplierQuotationLineResult> Lines)
{
    internal static SupplierQuotationResult FromDomain(SupplierQuotation quotation) => new(
        quotation.Id,
        quotation.SupplierId,
        quotation.Status,
        quotation.CurrencyCode,
        quotation.SupplierReference,
        quotation.ValidUntilDate,
        quotation.Notes,
        quotation.CreatedByUserId,
        quotation.CreatedAtUtc,
        quotation.FinalizedByUserId,
        quotation.FinalizedAtUtc,
        quotation.Lines
            .Select(line => new SupplierQuotationLineResult(
                line.Id,
                line.RequestForQuotationLineId,
                line.UnitPrice,
                line.PromisedDeliveryDate,
                line.Notes))
            .ToArray());
}

public sealed record RequestForQuotationResult(
    Guid Id,
    Guid PurchaseRequestId,
    RequestForQuotationStatus Status,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc,
    string? Notes,
    Guid? SentByUserId,
    DateTime? SentAtUtc,
    Guid? ClosedByUserId,
    DateTime? ClosedAtUtc,
    Guid? CancelledByUserId,
    DateTime? CancelledAtUtc,
    string? CancellationReason,
    IReadOnlyList<RequestForQuotationLineResult> Lines,
    IReadOnlyList<RequestForQuotationSupplierResult> Suppliers,
    IReadOnlyList<SupplierQuotationResult> SupplierQuotations)
{
    internal static RequestForQuotationResult FromDomain(RequestForQuotation requestForQuotation) => new(
        requestForQuotation.Id,
        requestForQuotation.PurchaseRequestId,
        requestForQuotation.Status,
        requestForQuotation.CreatedByUserId,
        requestForQuotation.CreatedAtUtc,
        requestForQuotation.Notes,
        requestForQuotation.SentByUserId,
        requestForQuotation.SentAtUtc,
        requestForQuotation.ClosedByUserId,
        requestForQuotation.ClosedAtUtc,
        requestForQuotation.CancelledByUserId,
        requestForQuotation.CancelledAtUtc,
        requestForQuotation.CancellationReason,
        requestForQuotation.Lines
            .OrderBy(line => line.LineNumber)
            .Select(line => new RequestForQuotationLineResult(line.Id, line.LineNumber, line.PurchaseRequestLineId))
            .ToArray(),
        requestForQuotation.Suppliers
            .OrderBy(supplier => supplier.SupplierId)
            .Select(supplier => new RequestForQuotationSupplierResult(supplier.Id, supplier.SupplierId))
            .ToArray(),
        requestForQuotation.SupplierQuotations
            .OrderBy(quotation => quotation.SupplierId)
            .Select(SupplierQuotationResult.FromDomain)
            .ToArray());
}
