using LocalErp.Domain.Common;

namespace LocalErp.Domain.Purchasing;

public sealed class PurchaseRequest : Entity
{
    public const int NotesMaxLength = 1000;
    public const int CancellationReasonMaxLength = 500;

    private readonly List<PurchaseRequestLine> _lines = [];

    private PurchaseRequest()
    {
    }

    public PurchaseRequest(Guid requesterUserId, DateTime createdAtUtc, string? notes = null)
    {
        RequesterUserId = ValidateIdentifier(requesterUserId, nameof(requesterUserId));
        CreatedAtUtc = ValidateUtc(createdAtUtc, nameof(createdAtUtc));
        Notes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));
        Status = PurchaseRequestStatus.Draft;
    }

    public PurchaseRequestStatus Status { get; private set; }

    public Guid RequesterUserId { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    public string? Notes { get; private set; }

    public DateTime? SubmittedAtUtc { get; private set; }

    public Guid? SubmittedByUserId { get; private set; }

    public DateTime? CancelledAtUtc { get; private set; }

    public Guid? CancelledByUserId { get; private set; }

    public string? CancellationReason { get; private set; }

    public IReadOnlyList<PurchaseRequestLine> Lines => _lines.AsReadOnly();

    public void UpdateNotes(string? notes)
    {
        EnsureDraft();
        Notes = NormalizeOptional(notes, NotesMaxLength, nameof(notes));
    }

    public PurchaseRequestLine AddLine(
        string description,
        decimal quantity,
        Guid measurementUnitId,
        DateOnly? neededByDate = null)
    {
        EnsureDraft();

        var lineNumber = _lines.Count == 0
            ? 1
            : checked(_lines.Max(line => line.LineNumber) + 1);

        var line = new PurchaseRequestLine(
            Id,
            lineNumber,
            description,
            quantity,
            measurementUnitId,
            neededByDate);

        _lines.Add(line);
        return line;
    }

    public void UpdateLine(
        Guid lineId,
        string description,
        decimal quantity,
        Guid measurementUnitId,
        DateOnly? neededByDate = null)
    {
        EnsureDraft();
        var line = GetRequiredLine(lineId);
        line.Update(description, quantity, measurementUnitId, neededByDate);
    }

    public void RemoveLine(Guid lineId)
    {
        EnsureDraft();
        var line = GetRequiredLine(lineId);
        _lines.Remove(line);
    }

    public void Submit(Guid actorUserId, DateTime submittedAtUtc)
    {
        EnsureDraft();
        if (_lines.Count == 0)
        {
            throw new InvalidOperationException("A purchase request must contain at least one line before submission.");
        }

        var validatedActorUserId = ValidateIdentifier(actorUserId, nameof(actorUserId));
        var validatedSubmittedAtUtc = ValidateUtc(submittedAtUtc, nameof(submittedAtUtc));

        SubmittedByUserId = validatedActorUserId;
        SubmittedAtUtc = validatedSubmittedAtUtc;
        Status = PurchaseRequestStatus.Submitted;
    }

    public void Cancel(Guid actorUserId, DateTime cancelledAtUtc, string reason)
    {
        if (Status is not PurchaseRequestStatus.Draft and not PurchaseRequestStatus.Submitted)
        {
            throw new InvalidOperationException("Only draft or submitted purchase requests can be cancelled.");
        }

        var validatedActorUserId = ValidateIdentifier(actorUserId, nameof(actorUserId));
        var validatedCancelledAtUtc = ValidateUtc(cancelledAtUtc, nameof(cancelledAtUtc));
        var normalizedReason = NormalizeRequired(reason, CancellationReasonMaxLength, nameof(reason));

        CancelledByUserId = validatedActorUserId;
        CancelledAtUtc = validatedCancelledAtUtc;
        CancellationReason = normalizedReason;
        Status = PurchaseRequestStatus.Cancelled;
    }

    private PurchaseRequestLine GetRequiredLine(Guid lineId)
    {
        if (lineId == Guid.Empty)
        {
            throw new ArgumentException("Purchase request line identifier cannot be empty.", nameof(lineId));
        }

        return _lines.SingleOrDefault(line => line.Id == lineId)
            ?? throw new KeyNotFoundException($"Purchase request line '{lineId}' was not found.");
    }

    private void EnsureDraft()
    {
        if (Status != PurchaseRequestStatus.Draft)
        {
            throw new InvalidOperationException("Only draft purchase requests can be edited.");
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
