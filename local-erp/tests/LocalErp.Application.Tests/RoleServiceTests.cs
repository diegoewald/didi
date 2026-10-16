using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Roles;
using LocalErp.Application.Security;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class RoleServiceTests
{
    [TestMethod]
    public async Task CreateAsyncRequiresRolesManagePermission()
    {
        var authorization = new RecordingAuthorizationService();
        var service = CreateService(authorization);

        await service.CreateAsync("Administradores");

        CollectionAssert.Contains(
            authorization.DemandedPermissions,
            PermissionCatalog.RolesManage);
    }

    [TestMethod]
    public async Task GrantPermissionAsyncIsIdempotentThroughRepositoryContract()
    {
        var roles = new InMemoryRoleRepository();
        var role = new Role("Operadores");
        await roles.AddAsync(role);
        var service = CreateService(new RecordingAuthorizationService(), roles);

        await service.GrantPermissionAsync(role.Id, PermissionCatalog.CompaniesRead);
        var result = await service.GrantPermissionAsync(role.Id, PermissionCatalog.CompaniesRead);

        Assert.AreEqual(1, result.Permissions.Count);
        Assert.AreEqual(PermissionCatalog.CompaniesRead, result.Permissions[0]);
    }

    [TestMethod]
    public async Task AssignUserAsyncRejectsInactiveUser()
    {
        var roles = new InMemoryRoleRepository();
        var role = new Role("Operadores");
        await roles.AddAsync(role);
        var users = new InMemoryUserRepository();
        var user = new User("inactive", "hash");
        user.Deactivate();
        users.Users.Add(user);
        var service = new RoleService(roles, users, new RecordingAuthorizationService());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.AssignUserAsync(user.Id, role.Id));
    }

    private static RoleService CreateService(
        RecordingAuthorizationService authorization,
        InMemoryRoleRepository? roles = null)
    {
        return new RoleService(
            roles ?? new InMemoryRoleRepository(),
            new InMemoryUserRepository(),
            authorization);
    }

    private sealed class RecordingAuthorizationService : IAuthorizationService
    {
        public List<string> DemandedPermissions { get; } = [];

        public Task<bool> IsAllowedAsync(string permissionCode, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(true);
        }

        public Task DemandAsync(string permissionCode, CancellationToken cancellationToken = default)
        {
            DemandedPermissions.Add(permissionCode);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRoleRepository : IRoleRepository
    {
        private readonly Dictionary<Guid, HashSet<string>> _permissions = [];
        private readonly HashSet<(Guid UserId, Guid RoleId)> _assignments = [];

        public List<Role> Roles { get; } = [];

        public Task AddAsync(Role role, CancellationToken cancellationToken = default)
        {
            Roles.Add(role);
            return Task.CompletedTask;
        }

        public Task<Role?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Roles.SingleOrDefault(role => role.Id == id));

        public Task<Role?> GetByNormalizedNameAsync(
            string normalizedName,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Roles.SingleOrDefault(role => role.NormalizedName == normalizedName));

        public Task<IReadOnlyList<Role>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<Role> result = Roles
                .Where(role => includeInactive || role.IsActive)
                .OrderBy(role => role.Name)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(Role role, CancellationToken cancellationToken = default)
            => Task.CompletedTask;

        public Task<IReadOnlyList<string>> GetPermissionsAsync(
            Guid roleId,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<string> result = _permissions.TryGetValue(roleId, out var permissions)
                ? permissions.OrderBy(code => code).ToArray()
                : [];
            return Task.FromResult(result);
        }

        public Task GrantPermissionAsync(
            Guid roleId,
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            if (!_permissions.TryGetValue(roleId, out var permissions))
            {
                permissions = [];
                _permissions.Add(roleId, permissions);
            }

            permissions.Add(permissionCode);
            return Task.CompletedTask;
        }

        public Task RevokePermissionAsync(
            Guid roleId,
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            if (_permissions.TryGetValue(roleId, out var permissions))
            {
                permissions.Remove(permissionCode);
            }

            return Task.CompletedTask;
        }

        public Task AssignUserAsync(
            Guid userId,
            Guid roleId,
            CancellationToken cancellationToken = default)
        {
            _assignments.Add((userId, roleId));
            return Task.CompletedTask;
        }

        public Task RemoveUserAsync(
            Guid userId,
            Guid roleId,
            CancellationToken cancellationToken = default)
        {
            _assignments.Remove((userId, roleId));
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
            => Task.FromResult(Users.SingleOrDefault(user => user.Id == id));

        public Task<User?> GetByNormalizedUsernameAsync(
            string normalizedUsername,
            CancellationToken cancellationToken = default)
            => Task.FromResult(Users.SingleOrDefault(user => user.NormalizedUsername == normalizedUsername));

        public Task<IReadOnlyList<User>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<User> result = Users.Where(user => includeInactive || user.IsActive).ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
