using LocalErp.Domain.Common;

namespace LocalErp.Domain.Security;

public sealed class SecurityAuditEvent : Entity
{
    public const int EventCodeMaxLength = 96;
    public const int DetailCodeMaxLength = 64;

    private SecurityAuditEvent()
    {
        EventCode = string.Empty;
    }

    public SecurityAuditEvent(
        DateTime occurredAtUtc,
        string eventCode,
        Guid? userId = null,
        string? detailCode = null,
        Guid? actorUserId = null,
        Guid? roleId = null)
    {
        if (occurredAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("Audit event timestamps must use UTC.", nameof(occurredAtUtc));
        }

        ValidateOptionalIdentifier(userId, nameof(userId));
        ValidateOptionalIdentifier(actorUserId, nameof(actorUserId));
        ValidateOptionalIdentifier(roleId, nameof(roleId));

        OccurredAtUtc = occurredAtUtc;
        EventCode = ValidateCode(eventCode, EventCodeMaxLength, nameof(eventCode));
        DetailCode = string.IsNullOrWhiteSpace(detailCode)
            ? null
            : ValidateCode(detailCode, DetailCodeMaxLength, nameof(detailCode));
        UserId = userId;
        ActorUserId = actorUserId;
        RoleId = roleId;
    }

    public DateTime OccurredAtUtc { get; private set; }

    public string EventCode { get; private set; } = string.Empty;

    public string? DetailCode { get; private set; }

    public Guid? UserId { get; private set; }

    public Guid? ActorUserId { get; private set; }

    public Guid? RoleId { get; private set; }

    private static void ValidateOptionalIdentifier(Guid? value, string parameterName)
    {
        if (value.HasValue)
        {
            ArgumentOutOfRangeException.ThrowIfEqual(value.Value, Guid.Empty, parameterName);
        }
    }

    private static string ValidateCode(string value, int maximumLength, string parameterName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(value, parameterName);

        var normalized = value.Trim();
        if (normalized.Length > maximumLength)
        {
            throw new ArgumentOutOfRangeException(
                parameterName,
                value,
                $"Audit code cannot exceed {maximumLength} characters.");
        }

        if (normalized.Any(character =>
            !((character is >= 'a' and <= 'z') ||
              (character is >= '0' and <= '9') ||
              character is '.' or '_' or '-')))
        {
            throw new ArgumentException(
                "Audit codes must use canonical lowercase ASCII letters, digits, '.', '_' and '-'.",
                parameterName);
        }

        return normalized;
    }
}
