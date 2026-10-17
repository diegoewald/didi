using LocalErp.Domain.Security;
using LocalErp.Domain.Users;

namespace LocalErp.Application.Abstractions.Persistence;

public interface IAdministrativeAuditStore
{
    Task AddUserAsync(
        User user,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default);

    Task UpdateUserAsync(
        User user,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default);

    Task<bool> AssignUserRoleAsync(
        Guid userId,
        Guid roleId,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default);

    Task<RoleAssignmentRemovalResult> RemoveUserRolePreservingPermissionHolderAsync(
        Guid userId,
        Guid roleId,
        string protectedPermissionCode,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default);
}
