using LocalErp.Domain.Security;

namespace LocalErp.Application.Abstractions.Persistence;

public interface IUserCredentialStore
{
    Task<bool> TryChangePasswordAsync(
        Guid userId,
        string expectedPasswordHash,
        string newPasswordHash,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default);
}
