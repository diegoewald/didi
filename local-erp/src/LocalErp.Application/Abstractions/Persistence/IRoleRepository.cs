using LocalErp.Domain.Security;

namespace LocalErp.Application.Abstractions.Persistence;

public interface IRoleRepository
{
    Task AddAsync(Role role, CancellationToken cancellationToken = default);

    Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<Role?> GetByNormalizedNameAsync(
        string normalizedName,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Role>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Role role, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetPermissionsAsync(
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task GrantPermissionAsync(
        Guid roleId,
        string permissionCode,
        CancellationToken cancellationToken = default);

    Task RevokePermissionAsync(
        Guid roleId,
        string permissionCode,
        CancellationToken cancellationToken = default);

    Task AssignUserAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default);

    Task RemoveUserAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default);
}
