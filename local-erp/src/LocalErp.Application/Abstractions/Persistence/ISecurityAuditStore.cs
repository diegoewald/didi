using LocalErp.Domain.Security;

namespace LocalErp.Application.Abstractions.Persistence;

public interface ISecurityAuditStore
{
    Task AppendAsync(
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default);
}
