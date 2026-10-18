using System.Data;
using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class SqliteAdministrativeAuditStore(
    IDbContextFactory<ErpDbContext> contextFactory) : IAdministrativeAuditStore
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task AddUserAsync(
        User user,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ValidateUserAuditEvent(user.Id, auditEvent);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Users.Add(user);
        context.SecurityAuditEvents.Add(auditEvent);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateUserAsync(
        User user,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        ValidateUserAuditEvent(user.Id, auditEvent);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Users.Update(user);
        context.SecurityAuditEvents.Add(auditEvent);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<bool> AssignUserRoleAsync(
        Guid userId,
        Guid roleId,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        ValidateUserRoleAuditEvent(userId, roleId, auditEvent);

        await using var context = await CreateImmediateTransactionalContextAsync(cancellationToken);
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Administrative role assignment transaction was not created.");

        try
        {
            var exists = await context.UserRoles.AnyAsync(
                assignment => assignment.UserId == userId && assignment.RoleId == roleId,
                cancellationToken);
            if (exists)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            context.UserRoles.Add(new UserRole(userId, roleId));
            context.SecurityAuditEvents.Add(auditEvent);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<RoleAssignmentRemovalResult> RemoveUserRolePreservingPermissionHolderAsync(
        Guid userId,
        Guid roleId,
        string protectedPermissionCode,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        ValidateUserRoleAuditEvent(userId, roleId, auditEvent);
        var normalizedPermission = PermissionCatalog.Validate(protectedPermissionCode);

        await using var context = await CreateImmediateTransactionalContextAsync(cancellationToken);
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Administrative role removal transaction was not created.");

        try
        {
            var assignment = await context.UserRoles.SingleOrDefaultAsync(
                entry => entry.UserId == userId && entry.RoleId == roleId,
                cancellationToken);
            if (assignment is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return RoleAssignmentRemovalResult.NotAssigned;
            }

            var contributesProtectedPermission = await (
                from user in context.Users
                join userRole in context.UserRoles on user.Id equals userRole.UserId
                join role in context.Roles on userRole.RoleId equals role.Id
                join permission in context.RolePermissions on role.Id equals permission.RoleId
                where userRole.UserId == userId
                    && userRole.RoleId == roleId
                    && user.IsActive
                    && role.IsActive
                    && permission.PermissionCode == normalizedPermission
                select userRole.UserId)
                .AnyAsync(cancellationToken);

            if (contributesProtectedPermission &&
                !await HasEffectivePermissionHolderAsync(
                    context,
                    normalizedPermission,
                    excludedUserId: userId,
                    excludedRoleId: roleId,
                    cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return RoleAssignmentRemovalResult.WouldRemoveLastPermissionHolder;
            }

            context.UserRoles.Remove(assignment);
            context.SecurityAuditEvents.Add(auditEvent);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return RoleAssignmentRemovalResult.Removed;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task AddRoleAsync(
        Role role,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);
        ValidateRoleAuditEvent(role.Id, auditEvent);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Roles.Add(role);
        context.SecurityAuditEvents.Add(auditEvent);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<ProtectedRoleMutationResult> SetRoleActivePreservingPermissionHolderAsync(
        Guid roleId,
        bool isActive,
        string protectedPermissionCode,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        ValidateRoleAuditEvent(roleId, auditEvent);
        var normalizedPermission = PermissionCatalog.Validate(protectedPermissionCode);

        await using var context = await CreateImmediateTransactionalContextAsync(cancellationToken);
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Administrative role status transaction was not created.");

        try
        {
            var role = await context.Roles.SingleOrDefaultAsync(
                entry => entry.Id == roleId,
                cancellationToken)
                ?? throw new KeyNotFoundException($"Role '{roleId}' was not found.");

            if (role.IsActive == isActive)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ProtectedRoleMutationResult.Unchanged;
            }

            if (!isActive &&
                await RoleContributesEffectivePermissionAsync(
                    context,
                    roleId,
                    normalizedPermission,
                    cancellationToken) &&
                !await HasEffectivePermissionHolderAsync(
                    context,
                    normalizedPermission,
                    excludedRoleId: roleId,
                    cancellationToken: cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return ProtectedRoleMutationResult.WouldRemoveLastPermissionHolder;
            }

            if (isActive)
            {
                role.Activate();
            }
            else
            {
                role.Deactivate();
            }

            context.SecurityAuditEvents.Add(auditEvent);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ProtectedRoleMutationResult.Changed;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<bool> GrantRolePermissionAsync(
        Guid roleId,
        string permissionCode,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        var normalizedPermission = PermissionCatalog.Validate(permissionCode);
        ValidatePermissionAuditEvent(roleId, normalizedPermission, auditEvent);

        await using var context = await CreateImmediateTransactionalContextAsync(cancellationToken);
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Administrative permission grant transaction was not created.");

        try
        {
            var exists = await context.RolePermissions.AnyAsync(
                permission => permission.RoleId == roleId && permission.PermissionCode == normalizedPermission,
                cancellationToken);
            if (exists)
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            context.RolePermissions.Add(new RolePermissionGrant(roleId, normalizedPermission));
            context.SecurityAuditEvents.Add(auditEvent);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<ProtectedRoleMutationResult> RevokeRolePermissionPreservingPermissionHolderAsync(
        Guid roleId,
        string permissionCode,
        string protectedPermissionCode,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        var normalizedPermission = PermissionCatalog.Validate(permissionCode);
        var normalizedProtectedPermission = PermissionCatalog.Validate(protectedPermissionCode);
        ValidatePermissionAuditEvent(roleId, normalizedPermission, auditEvent);

        await using var context = await CreateImmediateTransactionalContextAsync(cancellationToken);
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Administrative permission revocation transaction was not created.");

        try
        {
            var permission = await context.RolePermissions.SingleOrDefaultAsync(
                entry => entry.RoleId == roleId && entry.PermissionCode == normalizedPermission,
                cancellationToken);
            if (permission is null)
            {
                await transaction.RollbackAsync(cancellationToken);
                return ProtectedRoleMutationResult.Unchanged;
            }

            if (normalizedPermission == normalizedProtectedPermission &&
                await RoleContributesEffectivePermissionAsync(
                    context,
                    roleId,
                    normalizedProtectedPermission,
                    cancellationToken) &&
                !await HasEffectivePermissionHolderAsync(
                    context,
                    normalizedProtectedPermission,
                    excludedRoleId: roleId,
                    cancellationToken: cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return ProtectedRoleMutationResult.WouldRemoveLastPermissionHolder;
            }

            context.RolePermissions.Remove(permission);
            context.SecurityAuditEvents.Add(auditEvent);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return ProtectedRoleMutationResult.Changed;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private static async Task<bool> RoleContributesEffectivePermissionAsync(
        ErpDbContext context,
        Guid roleId,
        string permissionCode,
        CancellationToken cancellationToken)
    {
        return await (
            from user in context.Users
            join userRole in context.UserRoles on user.Id equals userRole.UserId
            join role in context.Roles on userRole.RoleId equals role.Id
            join permission in context.RolePermissions on role.Id equals permission.RoleId
            where role.Id == roleId
                && user.IsActive
                && role.IsActive
                && permission.PermissionCode == permissionCode
            select user.Id)
            .AnyAsync(cancellationToken);
    }

    private static async Task<bool> HasEffectivePermissionHolderAsync(
        ErpDbContext context,
        string permissionCode,
        Guid? excludedUserId = null,
        Guid? excludedRoleId = null,
        CancellationToken cancellationToken = default)
    {
        return await (
            from user in context.Users
            join userRole in context.UserRoles on user.Id equals userRole.UserId
            join role in context.Roles on userRole.RoleId equals role.Id
            join permission in context.RolePermissions on role.Id equals permission.RoleId
            where user.IsActive
                && role.IsActive
                && permission.PermissionCode == permissionCode
                && !(excludedUserId.HasValue &&
                    excludedRoleId.HasValue &&
                    userRole.UserId == excludedUserId.Value &&
                    userRole.RoleId == excludedRoleId.Value)
                && !(excludedRoleId.HasValue &&
                    !excludedUserId.HasValue &&
                    role.Id == excludedRoleId.Value)
            select user.Id)
            .AnyAsync(cancellationToken);
    }

    private static void ValidateUserAuditEvent(Guid userId, SecurityAuditEvent auditEvent)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        ArgumentNullException.ThrowIfNull(auditEvent);
        if (auditEvent.UserId != userId)
        {
            throw new ArgumentException("Audit event user must match the affected user.", nameof(auditEvent));
        }

        if (!auditEvent.ActorUserId.HasValue)
        {
            throw new ArgumentException("Administrative audit events require an actor user.", nameof(auditEvent));
        }
    }

    private static void ValidateUserRoleAuditEvent(
        Guid userId,
        Guid roleId,
        SecurityAuditEvent auditEvent)
    {
        ValidateUserAuditEvent(userId, auditEvent);
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role identifier cannot be empty.", nameof(roleId));
        }

        if (auditEvent.RoleId != roleId)
        {
            throw new ArgumentException("Audit event role must match the affected role.", nameof(auditEvent));
        }
    }

    private static void ValidateRoleAuditEvent(Guid roleId, SecurityAuditEvent auditEvent)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role identifier cannot be empty.", nameof(roleId));
        }

        ArgumentNullException.ThrowIfNull(auditEvent);
        if (auditEvent.UserId.HasValue)
        {
            throw new ArgumentException("Role-only audit events cannot identify an affected user.", nameof(auditEvent));
        }

        if (!auditEvent.ActorUserId.HasValue)
        {
            throw new ArgumentException("Administrative audit events require an actor user.", nameof(auditEvent));
        }

        if (auditEvent.RoleId != roleId)
        {
            throw new ArgumentException("Audit event role must match the affected role.", nameof(auditEvent));
        }
    }

    private static void ValidatePermissionAuditEvent(
        Guid roleId,
        string permissionCode,
        SecurityAuditEvent auditEvent)
    {
        ValidateRoleAuditEvent(roleId, auditEvent);
        if (!string.Equals(auditEvent.DetailCode, permissionCode, StringComparison.Ordinal))
        {
            throw new ArgumentException(
                "Permission audit event detail must match the affected permission.",
                nameof(auditEvent));
        }
    }

    private async Task<ErpDbContext> CreateImmediateTransactionalContextAsync(
        CancellationToken cancellationToken)
    {
        var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);

        if (context.Database.GetDbConnection() is not SqliteConnection connection)
        {
            await context.DisposeAsync();
            throw new InvalidOperationException(
                "Administrative audit persistence requires a Microsoft.Data.Sqlite connection.");
        }

        var transaction = connection.BeginTransaction(
            IsolationLevel.Serializable,
            deferred: false);
        context.Database.UseTransaction(transaction);
        return context;
    }
}
