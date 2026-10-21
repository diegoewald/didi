using LocalErp.Domain.Purchasing;

namespace LocalErp.Application.Purchasing;

public sealed record PurchaseRequestLineResult(
    Guid Id,
    int LineNumber,
    string Description,
    decimal Quantity,
    Guid MeasurementUnitId,
    DateOnly? NeededByDate)
{
    public static PurchaseRequestLineResult FromDomain(PurchaseRequestLine line)
    {
        ArgumentNullException.ThrowIfNull(line);
        return new PurchaseRequestLineResult(
            line.Id,
            line.LineNumber,
            line.Description,
            line.Quantity,
            line.MeasurementUnitId,
            line.NeededByDate);
    }
}

public sealed record PurchaseRequestResult(
    Guid Id,
    PurchaseRequestStatus Status,
    Guid RequesterUserId,
    DateTime CreatedAtUtc,
    string? Notes,
    DateTime? SubmittedAtUtc,
    Guid? SubmittedByUserId,
    DateTime? CancelledAtUtc,
    Guid? CancelledByUserId,
    string? CancellationReason,
    IReadOnlyList<PurchaseRequestLineResult> Lines)
{
    public static PurchaseRequestResult FromDomain(PurchaseRequest purchaseRequest)
    {
        ArgumentNullException.ThrowIfNull(purchaseRequest);
        return new PurchaseRequestResult(
            purchaseRequest.Id,
            purchaseRequest.Status,
            purchaseRequest.RequesterUserId,
            purchaseRequest.CreatedAtUtc,
            purchaseRequest.Notes,
            purchaseRequest.SubmittedAtUtc,
            purchaseRequest.SubmittedByUserId,
            purchaseRequest.CancelledAtUtc,
            purchaseRequest.CancelledByUserId,
            purchaseRequest.CancellationReason,
            purchaseRequest.Lines
                .OrderBy(line => line.LineNumber)
                .Select(PurchaseRequestLineResult.FromDomain)
                .ToArray());
    }
}
