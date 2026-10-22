using LocalErp.Domain.Common;

namespace LocalErp.Domain.Purchasing;

public sealed class RequestForQuotationAward : Entity
{
    public const int NotesMaxLength = 1000;

    private readonly List<RequestForQuotationAwardLine> _lines = [];

    private RequestForQuotationAward()
    {
    }

    public RequestForQuotationAward(
        Guid requestForQuotationId,
        Guid createdByUserId,
        DateTime createdAtUtc,
        string? notes = null)
    {
        RequestForQuotationId = ValidateIdentifier(requestForQuotationId, nameof(requestForQuotationId));
        CreatedByUserId = ValidateIdentifier(createdByUserId, nameof(createdByUserId));
        CreatedAtUtc = ValidateUtc(createdAtUtc, nameof(createdAtUtc));
        Notes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));
        Status = RequestForQuotationAwardStatus.Draft;
    }

    public Guid RequestForQuotationId { get; private set; }

    public RequestForQuotationAwardStatus Status { get; private set; }

    public Guid CreatedByUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public string? Notes { get; private set; }

    public Guid? FinalizedByUserId { get; private set; }

    public DateTime? FinalizedAtUtc { get; private set; }

    public IReadOnlyList<RequestForQuotationAwardLine> Lines => _lines.AsReadOnly();

    public void UpdateNotes(string? notes)
    {
        EnsureDraft();
        Notes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));
    }

    public RequestForQuotationAwardLine SelectLine(
        Guid requestForQuotationLineId,
        Guid supplierQuotationLineId)
    {
        EnsureDraft();
        var validatedRequestForQuotationLineId = ValidateIdentifier(
            requestForQuotationLineId,
            nameof(requestForQuotationLineId));
        var validatedSupplierQuotationLineId = ValidateIdentifier(
            supplierQuotationLineId,
            nameof(supplierQuotationLineId));

        if (_lines.Any(line =>
                line.RequestForQuotationLineId != validatedRequestForQuotationLineId &&
                line.SupplierQuotationLineId == validatedSupplierQuotationLineId))
        {
            throw new InvalidOperationException(
                "The same supplier quotation line cannot be awarded to multiple RFQ lines.");
        }

        var existing = _lines.SingleOrDefault(
            line => line.RequestForQuotationLineId == validatedRequestForQuotationLineId);
        if (existing is not null)
        {
            existing.ReplaceSupplierQuotationLine(validatedSupplierQuotationLineId);
            return existing;
        }

        var line = new RequestForQuotationAwardLine(
            Id,
            validatedRequestForQuotationLineId,
            validatedSupplierQuotationLineId);
        _lines.Add(line);
        return line;
    }

    public void RemoveLineSelection(Guid requestForQuotationLineId)
    {
        EnsureDraft();
        var validatedRequestForQuotationLineId = ValidateIdentifier(
            requestForQuotationLineId,
            nameof(requestForQuotationLineId));
        var line = _lines.SingleOrDefault(
            item => item.RequestForQuotationLineId == validatedRequestForQuotationLineId)
            ?? throw new KeyNotFoundException(
                $"RFQ line '{validatedRequestForQuotationLineId}' does not have an award selection.");
        _lines.Remove(line);
    }

    public void Finalize(Guid actorUserId, DateTime finalizedAtUtc)
    {
        EnsureDraft();
        if (_lines.Count == 0)
        {
            throw new InvalidOperationException(
                "An RFQ award decision requires at least one selected line before finalization.");
        }

        var validatedActorUserId = ValidateIdentifier(actorUserId, nameof(actorUserId));
        var validatedFinalizedAtUtc = ValidateUtc(finalizedAtUtc, nameof(finalizedAtUtc));

        FinalizedByUserId = validatedActorUserId;
        FinalizedAtUtc = validatedFinalizedAtUtc;
        Status = RequestForQuotationAwardStatus.Finalized;
    }

    private void EnsureDraft()
    {
        if (Status != RequestForQuotationAwardStatus.Draft)
        {
            throw new InvalidOperationException("Only draft RFQ award decisions can be edited.");
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
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Value cannot exceed {maxLength} characters.");
        }

        return normalized;
    }
}
