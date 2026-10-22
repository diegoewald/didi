using LocalErp.Domain.Common;

namespace LocalErp.Domain.Purchasing;

public sealed class PurchaseOrder : Entity
{
    public const int NotesMaxLength = 1000;
    public const int CancellationReasonMaxLength = 500;
    public const int SupplierNameMaxLength = 200;

    private readonly List<PurchaseOrderLine> _lines = [];

    private PurchaseOrder()
    {
        SupplierNameSnapshot = string.Empty;
        CurrencyCode = string.Empty;
    }

    public PurchaseOrder(
        int sequenceNumber,
        Guid requestForQuotationAwardId,
        Guid supplierId,
        string supplierName,
        string currencyCode,
        Guid createdByUserId,
        DateTime createdAtUtc,
        IEnumerable<PurchaseOrderLineSnapshot> lineSnapshots,
        string? notes = null)
    {
        ArgumentNullException.ThrowIfNull(lineSnapshots);
        var snapshots = lineSnapshots.ToArray();
        if (snapshots.Length == 0)
        {
            throw new InvalidOperationException("A purchase order must contain at least one awarded line.");
        }

        SequenceNumber = ValidateSequenceNumber(sequenceNumber);
        RequestForQuotationAwardId = ValidateIdentifier(
            requestForQuotationAwardId,
            nameof(requestForQuotationAwardId));
        SupplierId = ValidateIdentifier(supplierId, nameof(supplierId));
        SupplierNameSnapshot = NormalizeRequired(
            supplierName,
            SupplierNameMaxLength,
            nameof(supplierName));
        CurrencyCode = NormalizeCurrencyCode(currencyCode);
        CreatedByUserId = ValidateIdentifier(createdByUserId, nameof(createdByUserId));
        CreatedAtUtc = ValidateUtc(createdAtUtc, nameof(createdAtUtc));
        Notes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));
        Status = PurchaseOrderStatus.Draft;

        var duplicateAwardLine = snapshots
            .GroupBy(snapshot => snapshot.RequestForQuotationAwardLineId)
            .FirstOrDefault(group => group.Count() > 1);
        if (duplicateAwardLine is not null)
        {
            throw new InvalidOperationException(
                $"Award line '{duplicateAwardLine.Key}' cannot appear more than once in a purchase order.");
        }

        for (var index = 0; index < snapshots.Length; index++)
        {
            _lines.Add(new PurchaseOrderLine(Id, index + 1, snapshots[index]));
        }
    }

    public int SequenceNumber { get; private set; }

    public Guid RequestForQuotationAwardId { get; private set; }

    public Guid SupplierId { get; private set; }

    public string SupplierNameSnapshot { get; private set; }

    public string CurrencyCode { get; private set; }

    public PurchaseOrderStatus Status { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public string? Notes { get; private set; }

    public Guid? IssuedByUserId { get; private set; }

    public DateTime? IssuedAtUtc { get; private set; }

    public Guid? CancelledByUserId { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public string? CancellationReason { get; private set; }

    public IReadOnlyList<PurchaseOrderLine> Lines => _lines.AsReadOnly();

    public decimal Total => _lines.Sum(line => line.LineTotal);

    public void UpdateNotes(string? notes)
    {
        EnsureDraft();
        Notes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));
    }

    public void Issue(Guid actorUserId, DateTime issuedAtUtc)
    {
        EnsureDraft();
        if (_lines.Count == 0)
        {
            throw new InvalidOperationException("A purchase order must contain at least one line before issue.");
        }

        var validatedActorUserId = ValidateIdentifier(actorUserId, nameof(actorUserId));
        var validatedIssuedAtUtc = ValidateUtc(issuedAtUtc, nameof(issuedAtUtc));

        IssuedByUserId = validatedActorUserId;
        IssuedAtUtc = validatedIssuedAtUtc;
        Status = PurchaseOrderStatus.Issued;
    }

    public void Cancel(Guid actorUserId, DateTime cancelledAtUtc, string reason)
    {
        if (Status is not PurchaseOrderStatus.Draft and not PurchaseOrderStatus.Issued)
        {
            throw new InvalidOperationException("Only draft or issued purchase orders can be cancelled.");
        }

        var validatedActorUserId = ValidateIdentifier(actorUserId, nameof(actorUserId));
        var validatedCancelledAtUtc = ValidateUtc(cancelledAtUtc, nameof(cancelledAtUtc));
        var normalizedReason = NormalizeRequired(
            reason,
            CancellationReasonMaxLength,
            nameof(reason));

        CancelledByUserId = validatedActorUserId;
        CancelledAtUtc = validatedCancelledAtUtc;
        CancellationReason = normalizedReason;
        Status = PurchaseOrderStatus.Cancelled;
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseOrderStatus.Draft)
        {
            throw new InvalidOperationException("Only draft purchase orders can be edited or issued.");
        }
    }

    private static int ValidateSequenceNumber(int sequenceNumber)
    {
        if (sequenceNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(sequenceNumber),
                sequenceNumber,
                "Purchase order sequence number must be greater than zero.");
        }

        return sequenceNumber;
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

    private static string NormalizeCurrencyCode(string currencyCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currencyCode);
        var normalized = currencyCode.Trim().ToUpperInvariant();
        if (normalized.Length != 3 || normalized.Any(character => character is < 'A' or > 'Z'))
        {
            throw new ArgumentException(
                "Currency code must contain exactly three ASCII letters.",
                nameof(currencyCode));
        }

        return normalized;
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
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Value cannot exceed {maxLength} characters.");
        }

        return normalized;
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
