using LocalErp.Domain.Common;

namespace LocalErp.Domain.Purchasing;

public sealed class RequestForQuotation : Entity
{
    public const int NotesMaxLength = 1000;
    public const int CancellationReasonMaxLength = 500;

    private readonly List<RequestForQuotationLine> _lines = [];
    private readonly List<RequestForQuotationSupplier> _suppliers = [];
    private readonly List<SupplierQuotation> _supplierQuotations = [];

    private RequestForQuotation()
    {
    }

    public RequestForQuotation(
        Guid purchaseRequestId,
        Guid createdByUserId,
        DateTime createdAtUtc,
        string? notes = null)
    {
        PurchaseRequestId = ValidateIdentifier(purchaseRequestId, nameof(purchaseRequestId));
        CreatedByUserId = ValidateIdentifier(createdByUserId, nameof(createdByUserId));
        CreatedAtUtc = ValidateUtc(createdAtUtc, nameof(createdAtUtc));
        Notes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));
        Status = RequestForQuotationStatus.Draft;
    }

    public Guid PurchaseRequestId { get; private set; }

    public RequestForQuotationStatus Status { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public string? Notes { get; private set; }

    public Guid? SentByUserId { get; private set; }

    public DateTime? SentAtUtc { get; private set; }

    public Guid? ClosedByUserId { get; private set; }

    public DateTime? ClosedAtUtc { get; private set; }

    public Guid? CancelledByUserId { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public string? CancellationReason { get; private set; }

    public IReadOnlyList<RequestForQuotationLine> Lines => _lines.AsReadOnly();

    public IReadOnlyList<RequestForQuotationSupplier> Suppliers => _suppliers.AsReadOnly();

    public IReadOnlyList<SupplierQuotation> SupplierQuotations => _supplierQuotations.AsReadOnly();

    public void UpdateNotes(string? notes)
    {
        EnsureDraft();
        Notes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));
    }

    public RequestForQuotationLine AddSourceLine(Guid purchaseRequestLineId)
    {
        EnsureDraft();
        var validatedLineId = ValidateIdentifier(purchaseRequestLineId, nameof(purchaseRequestLineId));
        if (_lines.Any(line => line.PurchaseRequestLineId == validatedLineId))
        {
            throw new InvalidOperationException("The purchase request line is already included in this RFQ.");
        }

        var lineNumber = _lines.Count == 0 ? 1 : checked(_lines.Max(line => line.LineNumber) + 1);
        var line = new RequestForQuotationLine(Id, lineNumber, validatedLineId);
        _lines.Add(line);
        return line;
    }

    public void RemoveSourceLine(Guid requestForQuotationLineId)
    {
        EnsureDraft();
        var line = GetRequiredLine(requestForQuotationLineId);
        _lines.Remove(line);
    }

    public RequestForQuotationSupplier AddSupplier(Guid supplierId)
    {
        EnsureDraft();
        var validatedSupplierId = ValidateIdentifier(supplierId, nameof(supplierId));
        if (_suppliers.Any(supplier => supplier.SupplierId == validatedSupplierId))
        {
            throw new InvalidOperationException("The supplier is already invited to this RFQ.");
        }

        var supplier = new RequestForQuotationSupplier(Id, validatedSupplierId);
        _suppliers.Add(supplier);
        return supplier;
    }

    public void RemoveSupplier(Guid supplierId)
    {
        EnsureDraft();
        var validatedSupplierId = ValidateIdentifier(supplierId, nameof(supplierId));
        var supplier = _suppliers.SingleOrDefault(item => item.SupplierId == validatedSupplierId)
            ?? throw new KeyNotFoundException($"Supplier '{validatedSupplierId}' is not invited to this RFQ.");
        _suppliers.Remove(supplier);
    }

    public void Send(Guid actorUserId, DateTime sentAtUtc)
    {
        EnsureDraft();
        if (_lines.Count == 0)
        {
            throw new InvalidOperationException("An RFQ must contain at least one source line before it can be sent.");
        }

        if (_suppliers.Count == 0)
        {
            throw new InvalidOperationException("An RFQ must contain at least one invited supplier before it can be sent.");
        }

        var validatedActorUserId = ValidateIdentifier(actorUserId, nameof(actorUserId));
        var validatedSentAtUtc = ValidateUtc(sentAtUtc, nameof(sentAtUtc));

        SentByUserId = validatedActorUserId;
        SentAtUtc = validatedSentAtUtc;
        Status = RequestForQuotationStatus.Sent;
    }

    public SupplierQuotation CreateSupplierQuotation(
        Guid supplierId,
        string currencyCode,
        Guid createdByUserId,
        DateTime createdAtUtc,
        string? supplierReference = null,
        DateOnly? validUntilDate = null,
        string? notes = null)
    {
        EnsureSent();
        var validatedSupplierId = ValidateIdentifier(supplierId, nameof(supplierId));
        EnsureInvitedSupplier(validatedSupplierId);
        if (_supplierQuotations.Any(quotation => quotation.SupplierId == validatedSupplierId))
        {
            throw new InvalidOperationException("This supplier already has a quotation registered for the RFQ.");
        }

        var quotation = new SupplierQuotation(
            Id,
            validatedSupplierId,
            currencyCode,
            createdByUserId,
            createdAtUtc,
            supplierReference,
            validUntilDate,
            notes);
        _supplierQuotations.Add(quotation);
        return quotation;
    }

    public void UpdateSupplierQuotation(
        Guid supplierQuotationId,
        string currencyCode,
        string? supplierReference,
        DateOnly? validUntilDate,
        string? notes)
    {
        EnsureSent();
        GetRequiredSupplierQuotation(supplierQuotationId)
            .Update(currencyCode, supplierReference, validUntilDate, notes);
    }

    public SupplierQuotationLine UpsertSupplierQuotationLine(
        Guid supplierQuotationId,
        Guid requestForQuotationLineId,
        decimal unitPrice,
        DateOnly? promisedDeliveryDate = null,
        string? notes = null)
    {
        EnsureSent();
        _ = GetRequiredLine(requestForQuotationLineId);
        return GetRequiredSupplierQuotation(supplierQuotationId)
            .UpsertLine(requestForQuotationLineId, unitPrice, promisedDeliveryDate, notes);
    }

    public void RemoveSupplierQuotationLine(Guid supplierQuotationId, Guid supplierQuotationLineId)
    {
        EnsureSent();
        GetRequiredSupplierQuotation(supplierQuotationId).RemoveLine(supplierQuotationLineId);
    }

    public void FinalizeSupplierQuotation(Guid supplierQuotationId, Guid actorUserId, DateTime finalizedAtUtc)
    {
        EnsureSent();
        GetRequiredSupplierQuotation(supplierQuotationId).Finalize(actorUserId, finalizedAtUtc);
    }

    public void Close(Guid actorUserId, DateTime closedAtUtc)
    {
        EnsureSent();
        if (!_supplierQuotations.Any(quotation => quotation.Status == SupplierQuotationStatus.Finalized))
        {
            throw new InvalidOperationException("An RFQ requires at least one finalized supplier quotation before closing.");
        }

        var validatedActorUserId = ValidateIdentifier(actorUserId, nameof(actorUserId));
        var validatedClosedAtUtc = ValidateUtc(closedAtUtc, nameof(closedAtUtc));

        ClosedByUserId = validatedActorUserId;
        ClosedAtUtc = validatedClosedAtUtc;
        Status = RequestForQuotationStatus.Closed;
    }

    public void Cancel(Guid actorUserId, DateTime cancelledAtUtc, string reason)
    {
        if (Status is not RequestForQuotationStatus.Draft and not RequestForQuotationStatus.Sent)
        {
            throw new InvalidOperationException("Only draft or sent RFQs can be cancelled.");
        }

        var validatedActorUserId = ValidateIdentifier(actorUserId, nameof(actorUserId));
        var validatedCancelledAtUtc = ValidateUtc(cancelledAtUtc, nameof(cancelledAtUtc));
        var normalizedReason = NormalizeRequired(reason, CancellationReasonMaxLength, nameof(reason));

        CancelledByUserId = validatedActorUserId;
        CancelledAtUtc = validatedCancelledAtUtc;
        CancellationReason = normalizedReason;
        Status = RequestForQuotationStatus.Cancelled;
    }

    private RequestForQuotationLine GetRequiredLine(Guid requestForQuotationLineId)
    {
        if (requestForQuotationLineId == Guid.Empty)
        {
            throw new ArgumentException("RFQ line identifier cannot be empty.", nameof(requestForQuotationLineId));
        }

        return _lines.SingleOrDefault(line => line.Id == requestForQuotationLineId)
            ?? throw new KeyNotFoundException($"RFQ line '{requestForQuotationLineId}' was not found.");
    }

    private SupplierQuotation GetRequiredSupplierQuotation(Guid supplierQuotationId)
    {
        if (supplierQuotationId == Guid.Empty)
        {
            throw new ArgumentException("Supplier quotation identifier cannot be empty.", nameof(supplierQuotationId));
        }

        return _supplierQuotations.SingleOrDefault(quotation => quotation.Id == supplierQuotationId)
            ?? throw new KeyNotFoundException($"Supplier quotation '{supplierQuotationId}' was not found.");
    }

    private void EnsureInvitedSupplier(Guid supplierId)
    {
        if (!_suppliers.Any(supplier => supplier.SupplierId == supplierId))
        {
            throw new InvalidOperationException("Supplier quotations can only be registered for invited suppliers.");
        }
    }

    private void EnsureDraft()
    {
        if (Status != RequestForQuotationStatus.Draft)
        {
            throw new InvalidOperationException("Only draft RFQs can be edited.");
        }
    }

    private void EnsureSent()
    {
        if (Status != RequestForQuotationStatus.Sent)
        {
            throw new InvalidOperationException("This operation requires an RFQ in Sent status.");
        }
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

    private static string NormalizeRequired(string value, int maxLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);
        var normalized = value.Trim();
        if (normalized.Length > maxLength)
        {
            throw new ArgumentOutOfRangeException(parameterName, value, $"Value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
