using System.Data;
using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class EfRoleRepository(IDbContextFactory<ErpDbContext> contextFactory) : IRoleRepository
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task AddAsync(Role role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Roles.Add(role);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Roles.SingleOrDefaultAsync(role => role.Id == id, cancellationToken);
    }

    public async Task<Role?> GetByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedName);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Roles.SingleOrDefaultAsync(
            role => role.NormalizedName == normalizedName,
            cancellationToken);
    }

    public async Task<IReadOnlyList<Role>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Roles
            .Where(role => includeInactive || role.IsActive)
            .OrderBy(role => role.Name)
            .ToArrayAsync(cancellationToken);
    }

    public async Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(role);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Roles.Update(role);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<string>> GetPermissionsAsync(
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.RolePermissions
            .Where(permission => permission.RoleId == roleId)
            .OrderBy(permission => permission.PermissionCode)
            .Select(permission => permission.PermissionCode)
            .ToArrayAsync(cancellationToken);
    }

    public async Task GrantPermissionAsync(
        Guid roleId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedPermission = PermissionCatalog.Validate(permissionCode);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var exists = await context.RolePermissions.AnyAsync(
            permission => permission.RoleId == roleId && permission.PermissionCode == normalizedPermission,
            cancellationToken);
        if (exists)
        {
            return;
        }

        context.RolePermissions.Add(new RolePermissionGrant(roleId, normalizedPermission));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task RevokePermissionAsync(
        Guid roleId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedPermission = PermissionCatalog.Validate(permissionCode);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var permission = await context.RolePermissions.SingleOrDefaultAsync(
            entry => entry.RoleId == roleId && entry.PermissionCode == normalizedPermission,
            cancellationToken);
        if (permission is null)
        {
            return;
        }

        context.RolePermissions.Remove(permission);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<Guid>> GetRoleIdsForUserAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return [];
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.UserRoles
            .Where(assignment => assignment.UserId == userId)
            .OrderBy(assignment => assignment.RoleId)
            .Select(assignment => assignment.RoleId)
            .ToArrayAsync(cancellationToken);
    }

    public async Task AssignUserAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var exists = await context.UserRoles.AnyAsync(
            assignment => assignment.UserId == userId && assignment.RoleId == roleId,
            cancellationToken);
        if (exists)
        {
            return;
        }

        context.UserRoles.Add(new UserRole(userId, roleId));
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RoleAssignmentRemovalResult> RemoveUserPreservingPermissionHolderAsync(
        Guid userId,
        Guid roleId,
        string protectedPermissionCode,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role identifier cannot be empty.", nameof(roleId));
        }

        var normalizedPermission = PermissionCatalog.Validate(protectedPermissionCode);
        await using var context = await CreateImmediateTransactionalContextAsync(cancellationToken);
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Role assignment transaction was not created.");

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

    private async Task<ErpDbContext> CreateImmediateTransactionalContextAsync(
        CancellationToken cancellationToken)
    {
        var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);

        if (context.Database.GetDbConnection() is not SqliteConnection connection)
        {
            await context.DisposeAsync();
            throw new InvalidOperationException(
                "Role assignment persistence requires a Microsoft.Data.Sqlite connection.");
        }

        var transaction = connection.BeginTransaction(
            IsolationLevel.Serializable,
            deferred: false);
        context.Database.UseTransaction(transaction);
        return context;
    }
}
