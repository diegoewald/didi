using LocalErp.Domain.Common;

namespace LocalErp.Domain.Purchasing;

public sealed class PurchaseOrderLine : Entity
{
    public const int DescriptionMaxLength = 500;
    public const int MeasurementUnitNameMaxLength = 200;

    private PurchaseOrderLine()
    {
        DescriptionSnapshot = string.Empty;
        MeasurementUnitNameSnapshot = string.Empty;
    }

    internal PurchaseOrderLine(
        Guid purchaseOrderId,
        int lineNumber,
        PurchaseOrderLineSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        PurchaseOrderId = ValidateIdentifier(purchaseOrderId, nameof(purchaseOrderId));
        LineNumber = ValidateLineNumber(lineNumber);
        RequestForQuotationAwardLineId = ValidateIdentifier(
            snapshot.RequestForQuotationAwardLineId,
            nameof(snapshot.RequestForQuotationAwardLineId));
        DescriptionSnapshot = NormalizeRequired(
            snapshot.Description,
            DescriptionMaxLength,
            nameof(snapshot.Description));
        Quantity = ValidateQuantity(snapshot.Quantity);
        MeasurementUnitId = ValidateIdentifier(snapshot.MeasurementUnitId, nameof(snapshot.MeasurementUnitId));
        MeasurementUnitNameSnapshot = NormalizeRequired(
            snapshot.MeasurementUnitName,
            MeasurementUnitNameMaxLength,
            nameof(snapshot.MeasurementUnitName));
        UnitPrice = ValidateUnitPrice(snapshot.UnitPrice);
        PromisedDeliveryDate = snapshot.PromisedDeliveryDate;
    }

    public Guid PurchaseOrderId { get; private set; }

    public int LineNumber { get; private set; }

    public Guid RequestForQuotationAwardLineId { get; private set; }

    public string DescriptionSnapshot { get; private set; }

    public decimal Quantity { get; private set; }

    public Guid MeasurementUnitId { get; private set; }

    public string MeasurementUnitNameSnapshot { get; private set; }

    public decimal UnitPrice { get; private set; }

    public DateOnly? PromisedDeliveryDate { get; private set; }

    public decimal LineTotal => Quantity * UnitPrice;

    private static Guid ValidateIdentifier(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }

        return id;
    }

    private static int ValidateLineNumber(int lineNumber)
    {
        if (lineNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lineNumber),
                lineNumber,
                "Purchase order line number must be greater than zero.");
        }

        return lineNumber;
    }

    private static decimal ValidateQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity,
                "Purchase order quantity must be greater than zero.");
        }

        return quantity;
    }

    private static decimal ValidateUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                unitPrice,
                "Purchase order unit price cannot be negative.");
        }

        if (decimal.Round(unitPrice, 6, MidpointRounding.ToEven) != unitPrice)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                unitPrice,
                "Purchase order unit price cannot exceed six decimal places.");
        }

        return unitPrice;
    }

    private static string NormalizeRequired(string value, int maxLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
