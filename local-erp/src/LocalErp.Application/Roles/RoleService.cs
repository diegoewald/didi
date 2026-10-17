using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Security;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;

namespace LocalErp.Application.Roles;

public sealed class RoleService(
    IRoleRepository roleRepository,
    IUserRepository userRepository,
    IAuthorizationService authorizationService,
    IAdministrativeAuditStore administrativeAuditStore,
    IUserSession session,
    IClock clock)
{
    private readonly IRoleRepository _roleRepository = roleRepository
        ?? throw new ArgumentNullException(nameof(roleRepository));
    private readonly IUserRepository _userRepository = userRepository
        ?? throw new ArgumentNullException(nameof(userRepository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));
    private readonly IAdministrativeAuditStore _administrativeAuditStore = administrativeAuditStore
        ?? throw new ArgumentNullException(nameof(administrativeAuditStore));
    private readonly IUserSession _session = session ?? throw new ArgumentNullException(nameof(session));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public async Task<RoleResult> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RolesManage, cancellationToken);

        var normalizedName = Role.NormalizeName(name);
        if (await _roleRepository.GetByNormalizedNameAsync(normalizedName, cancellationToken) is not null)
        {
            throw new InvalidOperationException("A role with the same name already exists.");
        }

        var role = new Role(name);
        await _roleRepository.AddAsync(role, cancellationToken);
        return RoleResult.FromDomain(role, []);
    }

    public async Task<IReadOnlyList<RoleResult>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RolesRead, cancellationToken);

        var roles = await _roleRepository.ListAsync(includeInactive, cancellationToken);
        var results = new List<RoleResult>(roles.Count);
        foreach (var role in roles)
        {
            var permissions = await _roleRepository.GetPermissionsAsync(role.Id, cancellationToken);
            results.Add(RoleResult.FromDomain(role, permissions));
        }

        return results;
    }

    public async Task<IReadOnlyList<UserRoleAssignmentResult>> ListUserAssignmentsAsync(
        Guid userId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RolesRead, cancellationToken);
        var user = await GetRequiredUserAsync(userId, cancellationToken);
        var roles = await _roleRepository.ListAsync(includeInactive: true, cancellationToken);
        var assignedRoleIds = (await _roleRepository.GetRoleIdsForUserAsync(user.Id, cancellationToken)).ToHashSet();

        return roles
            .Select(role => UserRoleAssignmentResult.FromDomain(role, assignedRoleIds.Contains(role.Id)))
            .ToArray();
    }

    public async Task<RoleResult> GrantPermissionAsync(
        Guid roleId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RolesManage, cancellationToken);
        var role = await GetRequiredRoleAsync(roleId, cancellationToken);
        var normalizedPermission = PermissionCatalog.Validate(permissionCode);

        await _roleRepository.GrantPermissionAsync(role.Id, normalizedPermission, cancellationToken);
        var permissions = await _roleRepository.GetPermissionsAsync(role.Id, cancellationToken);
        return RoleResult.FromDomain(role, permissions);
    }

    public async Task<RoleResult> RevokePermissionAsync(
        Guid roleId,
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RolesManage, cancellationToken);
        var role = await GetRequiredRoleAsync(roleId, cancellationToken);
        var normalizedPermission = PermissionCatalog.Validate(permissionCode);

        await _roleRepository.RevokePermissionAsync(role.Id, normalizedPermission, cancellationToken);
        var permissions = await _roleRepository.GetPermissionsAsync(role.Id, cancellationToken);
        return RoleResult.FromDomain(role, permissions);
    }

    public async Task AssignUserAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RolesManage, cancellationToken);
        var user = await GetRequiredUserAsync(userId, cancellationToken);
        var role = await GetRequiredRoleAsync(roleId, cancellationToken);

        if (!user.IsActive)
        {
            throw new InvalidOperationException("Inactive users cannot be assigned to roles.");
        }

        if (!role.IsActive)
        {
            throw new InvalidOperationException("Users cannot be assigned to inactive roles.");
        }

        var auditEvent = CreateRoleAuditEvent(
            SecurityAuditCodes.AdministrationUserRoleAssigned,
            user.Id,
            role.Id);
        await _administrativeAuditStore.AssignUserRoleAsync(
            user.Id,
            role.Id,
            auditEvent,
            cancellationToken);
    }

    public async Task RemoveUserAsync(
        Guid userId,
        Guid roleId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RolesManage, cancellationToken);
        var user = await GetRequiredUserAsync(userId, cancellationToken);
        var role = await GetRequiredRoleAsync(roleId, cancellationToken);

        var auditEvent = CreateRoleAuditEvent(
            SecurityAuditCodes.AdministrationUserRoleRemoved,
            user.Id,
            role.Id);
        var removalResult = await _administrativeAuditStore.RemoveUserRolePreservingPermissionHolderAsync(
            user.Id,
            role.Id,
            PermissionCatalog.RolesManage,
            auditEvent,
            cancellationToken);

        if (removalResult == RoleAssignmentRemovalResult.WouldRemoveLastPermissionHolder)
        {
            throw new LastAdministrativeRoleAssignmentException();
        }
    }

    public async Task<RoleResult> SetActiveAsync(
        Guid roleId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RolesManage, cancellationToken);
        var role = await GetRequiredRoleAsync(roleId, cancellationToken);

        if (isActive)
        {
            role.Activate();
        }
        else
        {
            role.Deactivate();
        }

        await _roleRepository.UpdateAsync(role, cancellationToken);
        var permissions = await _roleRepository.GetPermissionsAsync(role.Id, cancellationToken);
        return RoleResult.FromDomain(role, permissions);
    }

    private SecurityAuditEvent CreateRoleAuditEvent(string eventCode, Guid userId, Guid roleId)
    {
        var actor = _session.CurrentUser
            ?? throw new InvalidOperationException("Administrative operations require an authenticated user.");

        return new SecurityAuditEvent(
            _clock.UtcNow,
            eventCode,
            userId: userId,
            actorUserId: actor.Id,
            roleId: roleId);
    }

    private async Task<User> GetRequiredUserAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        return await _userRepository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{userId}' was not found.");
    }

    private async Task<Role> GetRequiredRoleAsync(Guid roleId, CancellationToken cancellationToken)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role identifier cannot be empty.", nameof(roleId));
        }

        return await _roleRepository.GetByIdAsync(roleId, cancellationToken)
            ?? throw new KeyNotFoundException($"Role '{roleId}' was not found.");
    }
}
