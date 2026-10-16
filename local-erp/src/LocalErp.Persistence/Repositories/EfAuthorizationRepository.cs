using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class EfAuthorizationRepository(IDbContextFactory<ErpDbContext> contextFactory)
    : IAuthorizationRepository
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task<bool> UserHasPermissionAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        var normalizedPermission = PermissionCatalog.Validate(permissionCode);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        return await (
            from user in context.Users
            join assignment in context.UserRoles on user.Id equals assignment.UserId
            join role in context.Roles on assignment.RoleId equals role.Id
            join permission in context.RolePermissions on role.Id equals permission.RoleId
            where user.Id == userId
                && user.IsActive
                && role.IsActive
                && permission.PermissionCode == normalizedPermission
            select user.Id)
            .AnyAsync(cancellationToken);
    }
}
