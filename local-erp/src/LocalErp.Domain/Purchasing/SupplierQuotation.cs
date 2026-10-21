using LocalErp.Domain.Common;

namespace LocalErp.Domain.Purchasing;

public sealed class SupplierQuotation : Entity
{
    public const int SupplierReferenceMaxLength = 100;
    public const int NotesMaxLength = 1000;

    private readonly List<SupplierQuotationLine> _lines = [];

    private SupplierQuotation()
    {
        CurrencyCode = string.Empty;
    }

    internal SupplierQuotation(
        Guid requestForQuotationId,
        Guid supplierId,
        string currencyCode,
        Guid createdByUserId,
        DateTime createdAtUtc,
        string? supplierReference = null,
        DateOnly? validUntilDate = null,
        string? notes = null)
    {
        RequestForQuotationId = ValidateIdentifier(requestForQuotationId, nameof(requestForQuotationId));
        SupplierId = ValidateIdentifier(supplierId, nameof(supplierId));
        CurrencyCode = NormalizeCurrencyCode(currencyCode);
        CreatedByUserId = ValidateIdentifier(createdByUserId, nameof(createdByUserId));
        CreatedAtUtc = ValidateUtc(createdAtUtc, nameof(createdAtUtc));
        SupplierReference = NormalizeOptional(supplierReference, SupplierReferenceMaxLength, nameof(supplierReference));
        ValidUntilDate = validUntilDate;
        Notes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));
        Status = SupplierQuotationStatus.Draft;
    }

    public Guid RequestForQuotationId { get; private set; }

    public Guid SupplierId { get; private set; }

    public SupplierQuotationStatus Status { get; private set; }

    public string CurrencyCode { get; private set; }

    public string? SupplierReference { get; private set; }

    public DateOnly? ValidUntilDate { get; private set; }

    public string? Notes { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public Guid? FinalizedByUserId { get; private set; }

    public DateTime? FinalizedAtUtc { get; private set; }

    public IReadOnlyList<SupplierQuotationLine> Lines => _lines.AsReadOnly();

    internal void Update(
        string currencyCode,
        string? supplierReference,
        DateOnly? validUntilDate,
        string? notes)
    {
        EnsureDraft();

        var normalizedCurrencyCode = NormalizeCurrencyCode(currencyCode);
        var normalizedSupplierReference = NormalizeOptional(
            supplierReference,
            SupplierReferenceMaxLength,
            nameof(supplierReference));
        var normalizedNotes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));

        CurrencyCode = normalizedCurrencyCode;
        SupplierReference = normalizedSupplierReference;
        ValidUntilDate = validUntilDate;
        Notes = normalizedNotes;
    }

    internal SupplierQuotationLine UpsertLine(
        Guid requestForQuotationLineId,
        decimal unitPrice,
        DateOnly? promisedDeliveryDate,
        string? notes)
    {
        EnsureDraft();
        var validatedLineId = ValidateIdentifier(requestForQuotationLineId, nameof(requestForQuotationLineId));
        var existing = _lines.SingleOrDefault(line => line.RequestForQuotationLineId == validatedLineId);
        if (existing is not null)
        {
            existing.Update(unitPrice, promisedDeliveryDate, notes);
            return existing;
        }

        var line = new SupplierQuotationLine(Id, validatedLineId, unitPrice, promisedDeliveryDate, notes);
        _lines.Add(line);
        return line;
    }

    internal void RemoveLine(Guid supplierQuotationLineId)
    {
        EnsureDraft();
        if (supplierQuotationLineId == Guid.Empty)
        {
            throw new ArgumentException("Supplier quotation line identifier cannot be empty.", nameof(supplierQuotationLineId));
        }

        var line = _lines.SingleOrDefault(item => item.Id == supplierQuotationLineId)
            ?? throw new KeyNotFoundException($"Supplier quotation line '{supplierQuotationLineId}' was not found.");
        _lines.Remove(line);
    }

    internal void Finalize(Guid actorUserId, DateTime finalizedAtUtc)
    {
        EnsureDraft();
        if (_lines.Count == 0)
        {
            throw new InvalidOperationException("A supplier quotation must contain at least one quoted line before finalization.");
        }

        var validatedActorUserId = ValidateIdentifier(actorUserId, nameof(actorUserId));
        var validatedFinalizedAtUtc = ValidateUtc(finalizedAtUtc, nameof(finalizedAtUtc));

        FinalizedByUserId = validatedActorUserId;
        FinalizedAtUtc = validatedFinalizedAtUtc;
        Status = SupplierQuotationStatus.Finalized;
    }

    private void EnsureDraft()
    {
        if (Status != SupplierQuotationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft supplier quotations can be edited.");
        }
    }

    private static string NormalizeCurrencyCode(string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException("Currency code must contain exactly three ASCII letters.", nameof(currencyCode));
        }

        return normalized;
    }

    private static Guid ValidateIdentifier(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }

        return id;
    }

    private static DateTime ValidateUtc(DateTime value, string parameterName)
    {
        if (value.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Timestamp must use UTC.", parameterName);
        }

        return value;
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
