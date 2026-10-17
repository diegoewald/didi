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
        ValidateRoleAuditEvent(userId, roleId, auditEvent);

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
        ValidateRoleAuditEvent(userId, roleId, auditEvent);
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

            if (contributesProtectedPermission)
            {
                var hasRemainingPermissionHolder = await (
                    from user in context.Users
                    join userRole in context.UserRoles on user.Id equals userRole.UserId
                    join role in context.Roles on userRole.RoleId equals role.Id
                    join permission in context.RolePermissions on role.Id equals permission.RoleId
                    where user.IsActive
                        && role.IsActive
                        && permission.PermissionCode == normalizedPermission
                        && !(userRole.UserId == userId && userRole.RoleId == roleId)
                    select userRole.UserId)
                    .AnyAsync(cancellationToken);

                if (!hasRemainingPermissionHolder)
                {
                    await transaction.RollbackAsync(cancellationToken);
                    return RoleAssignmentRemovalResult.WouldRemoveLastPermissionHolder;
                }
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

    private static void ValidateRoleAuditEvent(
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
