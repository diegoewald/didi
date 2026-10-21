using LocalErp.Domain.Common;

namespace LocalErp.Domain.Purchasing;

public sealed class PurchaseRequestLine : Entity
{
    public const int DescriptionMaxLength = 500;

    private PurchaseRequestLine()
    {
        Description = string.Empty;
    }

    internal PurchaseRequestLine(
        Guid purchaseRequestId,
        int lineNumber,
        string description,
        decimal quantity,
        Guid measurementUnitId,
        DateOnly? neededByDate)
    {
        PurchaseRequestId = ValidateIdentifier(purchaseRequestId, nameof(purchaseRequestId));
        LineNumber = ValidateLineNumber(lineNumber);
        Description = NormalizeDescription(description);
        Quantity = ValidateQuantity(quantity);
        MeasurementUnitId = ValidateIdentifier(measurementUnitId, nameof(measurementUnitId));
        NeededByDate = neededByDate;
    }

    public Guid PurchaseRequestId { get; private set; }

    public int LineNumber { get; private set; }

    public string Description { get; private set; }

    public decimal Quantity { get; private set; }

    public Guid MeasurementUnitId { get; private set; }

    public DateOnly? NeededByDate { get; private set; }

    internal void Update(
        string description,
        decimal quantity,
        Guid measurementUnitId,
        DateOnly? neededByDate)
    {
        var normalizedDescription = NormalizeDescription(description);
        var validatedQuantity = ValidateQuantity(quantity);
        var validatedMeasurementUnitId = ValidateIdentifier(measurementUnitId, nameof(measurementUnitId));

        Description = normalizedDescription;
        Quantity = validatedQuantity;
        MeasurementUnitId = validatedMeasurementUnitId;
        NeededByDate = neededByDate;
    }

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
                "Purchase request line number must be greater than zero.");
        }

        return lineNumber;
    }

    private static string NormalizeDescription(string description)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(description);

        var normalized = description.Trim();
        if (normalized.Length > DescriptionMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(description),
                description,
                $"Purchase request line description cannot exceed {DescriptionMaxLength} characters.");
        }

        return normalized;
    }

    private static decimal ValidateQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(quantity),
                quantity,
                "Purchase request quantity must be greater than zero.");
        }

        return quantity;
    }
}
