using LocalErp.Domain.Common;

namespace LocalErp.Domain.Purchasing;

public sealed class SupplierQuotationLine : Entity
{
    public const int NotesMaxLength = 500;

    private SupplierQuotationLine()
    {
    }

    internal SupplierQuotationLine(
        Guid supplierQuotationId,
        Guid requestForQuotationLineId,
        decimal unitPrice,
        DateOnly? promisedDeliveryDate,
        string? notes)
    {
        SupplierQuotationId = ValidateIdentifier(supplierQuotationId, nameof(supplierQuotationId));
        RequestForQuotationLineId = ValidateIdentifier(requestForQuotationLineId, nameof(requestForQuotationLineId));
        UnitPrice = ValidateUnitPrice(unitPrice);
        PromisedDeliveryDate = promisedDeliveryDate;
        Notes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));
    }

    public Guid SupplierQuotationId { get; private set; }

    public Guid RequestForQuotationLineId { get; private set; }

    public decimal UnitPrice { get; private set; }

    public DateOnly? PromisedDeliveryDate { get; private set; }

    public string? Notes { get; private set; }

    internal void Update(decimal unitPrice, DateOnly? promisedDeliveryDate, string? notes)
    {
        var validatedUnitPrice = ValidateUnitPrice(unitPrice);
        var normalizedNotes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));

        UnitPrice = validatedUnitPrice;
        PromisedDeliveryDate = promisedDeliveryDate;
        Notes = normalizedNotes;
    }

    private static decimal ValidateUnitPrice(decimal unitPrice)
    {
        if (unitPrice < 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                unitPrice,
                "Supplier quotation unit price cannot be negative.");
        }

        if (decimal.Round(unitPrice, 6, MidpointRounding.ToEven) != unitPrice)
        {
            throw new ArgumentOutOfRangeException(
                nameof(unitPrice),
                unitPrice,
                "Supplier quotation unit price cannot exceed six decimal places.");
        }

        return unitPrice;
    }

    private static Guid ValidateIdentifier(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }

        return id;
    }

    private static string? NormalizeOptional(string? value, int maxLength, string parameterName)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return null;
        }

        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"Value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
