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

    Task AddRoleAsync(
        Role role,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Administrative role creation persistence is not implemented.");

    Task<ProtectedRoleMutationResult> SetRoleActivePreservingPermissionHolderAsync(
        Guid roleId,
        bool isActive,
        string protectedPermissionCode,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Administrative role status persistence is not implemented.");

    Task<bool> GrantRolePermissionAsync(
        Guid roleId,
        string permissionCode,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Administrative permission grant persistence is not implemented.");

    Task<ProtectedRoleMutationResult> RevokeRolePermissionPreservingPermissionHolderAsync(
        Guid roleId,
        string permissionCode,
        string protectedPermissionCode,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
        => throw new NotSupportedException("Administrative permission revocation persistence is not implemented.");
}
