namespace LocalErp.Domain.Purchasing;

public sealed record PurchaseOrderLineSnapshot(
    Guid RequestForQuotationAwardLineId,
    string Description,
    decimal Quantity,
    Guid MeasurementUnitId,
    string MeasurementUnitName,
    decimal UnitPrice,
    DateOnly? PromisedDeliveryDate);
