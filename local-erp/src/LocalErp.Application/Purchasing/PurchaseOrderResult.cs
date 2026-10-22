using LocalErp.Domain.Purchasing;

namespace LocalErp.Application.Purchasing;

public sealed record PurchaseOrderLineResult(
    Guid Id,
    int LineNumber,
    Guid RequestForQuotationAwardLineId,
    string DescriptionSnapshot,
    decimal Quantity,
    Guid MeasurementUnitId,
    string MeasurementUnitNameSnapshot,
    decimal UnitPrice,
    DateOnly? PromisedDeliveryDate,
    decimal LineTotal);

public sealed record PurchaseOrderResult(
    Guid Id,
    int SequenceNumber,
    Guid RequestForQuotationAwardId,
    Guid SupplierId,
    string SupplierNameSnapshot,
    string CurrencyCode,
    PurchaseOrderStatus Status,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc,
    string? Notes,
    Guid? IssuedByUserId,
    DateTime? IssuedAtUtc,
    Guid? CancelledByUserId,
    DateTime? CancelledAtUtc,
    string? CancellationReason,
    decimal Total,
    IReadOnlyList<PurchaseOrderLineResult> Lines)
{
    internal static PurchaseOrderResult FromDomain(PurchaseOrder purchaseOrder)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrder);
        return new PurchaseOrderResult(
            purchaseOrder.Id,
            purchaseOrder.SequenceNumber,
            purchaseOrder.RequestForQuotationAwardId,
            purchaseOrder.SupplierId,
            purchaseOrder.SupplierNameSnapshot,
            purchaseOrder.CurrencyCode,
            purchaseOrder.Status,
            purchaseOrder.CreatedByUserId,
            purchaseOrder.CreatedAtUtc,
            purchaseOrder.Notes,
            purchaseOrder.IssuedByUserId,
            purchaseOrder.IssuedAtUtc,
            purchaseOrder.CancelledByUserId,
            purchaseOrder.CancelledAtUtc,
            purchaseOrder.CancellationReason,
            purchaseOrder.Total,
            purchaseOrder.Lines
                .OrderBy(line => line.LineNumber)
                .ThenBy(line => line.Id)
                .Select(line => new PurchaseOrderLineResult(
                    line.Id,
                    line.LineNumber,
                    line.RequestForQuotationAwardLineId,
                    line.DescriptionSnapshot,
                    line.Quantity,
                    line.MeasurementUnitId,
                    line.MeasurementUnitNameSnapshot,
                    line.UnitPrice,
                    line.PromisedDeliveryDate,
                    line.LineTotal))
                .ToArray());
    }
}
