using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using LocalErp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class EfRoleAuthorizationTests
{
    [TestMethod]
    public async Task RoleRepositoryPersistsPermissionsAndAssignmentsIdempotently()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var roles = new EfRoleRepository(database);

        var user = new User("operator", "hash");
        var role = new Role("Operators");
        await users.AddAsync(user);
        await roles.AddAsync(role);

        await roles.GrantPermissionAsync(role.Id, PermissionCatalog.CompaniesRead);
        await roles.GrantPermissionAsync(role.Id, PermissionCatalog.CompaniesRead);
        await roles.AssignUserAsync(user.Id, role.Id);
        await roles.AssignUserAsync(user.Id, role.Id);

        var permissions = await roles.GetPermissionsAsync(role.Id);
        Assert.AreEqual(1, permissions.Count);
        Assert.AreEqual(PermissionCatalog.CompaniesRead, permissions[0]);

        var assignedRoleIds = await roles.GetRoleIdsForUserAsync(user.Id);
        Assert.AreEqual(1, assignedRoleIds.Count);
        Assert.AreEqual(role.Id, assignedRoleIds[0]);

        await using var context = database.CreateDbContext();
        Assert.AreEqual(1, await context.UserRoles.CountAsync());
        Assert.AreEqual(1, await context.RolePermissions.CountAsync());
    }

    [TestMethod]
    public async Task AuthorizationRepositoryRequiresActiveUserAndActiveRole()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var roles = new EfRoleRepository(database);
        var authorization = new EfAuthorizationRepository(database);

        var user = new User("operator", "hash");
        var role = new Role("Operators");
        await users.AddAsync(user);
        await roles.AddAsync(role);
        await roles.GrantPermissionAsync(role.Id, PermissionCatalog.CompaniesRead);
        await roles.AssignUserAsync(user.Id, role.Id);

        Assert.IsTrue(await authorization.UserHasPermissionAsync(
            user.Id,
            PermissionCatalog.CompaniesRead));

        role.Deactivate();
        await roles.UpdateAsync(role);

        Assert.IsFalse(await authorization.UserHasPermissionAsync(
            user.Id,
            PermissionCatalog.CompaniesRead));
    }

    [TestMethod]
    public async Task RoleRepositoryBlocksRemovingLastEffectiveRolesManager()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var roles = new EfRoleRepository(database);

        var user = new User("admin", "hash");
        var role = new Role("Administrators");
        await users.AddAsync(user);
        await roles.AddAsync(role);
        await roles.GrantPermissionAsync(role.Id, PermissionCatalog.RolesManage);
        await roles.AssignUserAsync(user.Id, role.Id);

        var result = await roles.RemoveUserPreservingPermissionHolderAsync(
            user.Id,
            role.Id,
            PermissionCatalog.RolesManage);

        Assert.AreEqual(RoleAssignmentRemovalResult.WouldRemoveLastPermissionHolder, result);
        await using var context = database.CreateDbContext();
        Assert.AreEqual(1, await context.UserRoles.CountAsync());
    }

    [TestMethod]
    public async Task RoleRepositoryAllowsRemovalWhenAnotherEffectiveRolesManagerRemains()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var roles = new EfRoleRepository(database);
        var authorization = new EfAuthorizationRepository(database);

        var firstUser = new User("admin-one", "hash");
        var secondUser = new User("admin-two", "hash");
        var role = new Role("Administrators");
        await users.AddAsync(firstUser);
        await users.AddAsync(secondUser);
        await roles.AddAsync(role);
        await roles.GrantPermissionAsync(role.Id, PermissionCatalog.RolesManage);
        await roles.AssignUserAsync(firstUser.Id, role.Id);
        await roles.AssignUserAsync(secondUser.Id, role.Id);

        var result = await roles.RemoveUserPreservingPermissionHolderAsync(
            firstUser.Id,
            role.Id,
            PermissionCatalog.RolesManage);

        Assert.AreEqual(RoleAssignmentRemovalResult.Removed, result);
        Assert.IsFalse(await authorization.UserHasPermissionAsync(firstUser.Id, PermissionCatalog.RolesManage));
        Assert.IsTrue(await authorization.UserHasPermissionAsync(secondUser.Id, PermissionCatalog.RolesManage));
    }

    [TestMethod]
    public async Task ConcurrentAdministrativeRoleRemovalsPreserveOneEffectiveManager()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var roles = new EfRoleRepository(database);

        var firstUser = new User("admin-one", "hash");
        var secondUser = new User("admin-two", "hash");
        var role = new Role("Administrators");
        await users.AddAsync(firstUser);
        await users.AddAsync(secondUser);
        await roles.AddAsync(role);
        await roles.GrantPermissionAsync(role.Id, PermissionCatalog.RolesManage);
        await roles.AssignUserAsync(firstUser.Id, role.Id);
        await roles.AssignUserAsync(secondUser.Id, role.Id);

        var results = await Task.WhenAll(
            roles.RemoveUserPreservingPermissionHolderAsync(
                firstUser.Id,
                role.Id,
                PermissionCatalog.RolesManage),
            roles.RemoveUserPreservingPermissionHolderAsync(
                secondUser.Id,
                role.Id,
                PermissionCatalog.RolesManage));

        Assert.AreEqual(1, results.Count(result => result == RoleAssignmentRemovalResult.Removed));
        Assert.AreEqual(
            1,
            results.Count(result => result == RoleAssignmentRemovalResult.WouldRemoveLastPermissionHolder));

        await using var context = database.CreateDbContext();
        Assert.AreEqual(1, await context.UserRoles.CountAsync());
    }

    [TestMethod]
    public async Task DatabaseRejectsRoleAssignmentWithMissingForeignKey()
    {
        await using var database = await TestDatabase.CreateAsync();
        await using var context = database.CreateDbContext();
        context.UserRoles.Add(new UserRole(Guid.NewGuid(), Guid.NewGuid()));

        await Assert.ThrowsExactlyAsync<DbUpdateException>(
            () => context.SaveChangesAsync());
    }

    private sealed class TestDatabase : IDbContextFactory<ErpDbContext>, IAsyncDisposable
    {
        private readonly string _directory;
        private readonly DbContextOptions<ErpDbContext> _options;

        private TestDatabase(string directory, DbContextOptions<ErpDbContext> options)
        {
            _directory = directory;
            _options = options;
        }

        public static async Task<TestDatabase> CreateAsync()
        {
            var directory = Path.Combine(Path.GetTempPath(), "LocalErp", "Tests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(directory);
            var databasePath = Path.Combine(directory, "rbac.db");
            var connectionString = $"{SqliteConnectionStringFactory.Create(databasePath)};Pooling=False";
            var options = new DbContextOptionsBuilder<ErpDbContext>()
                .UseSqlite(connectionString)
                .Options;

            var database = new TestDatabase(directory, options);
            await using var context = database.CreateDbContext();
            await context.Database.MigrateAsync();
            return database;
        }

        public ErpDbContext CreateDbContext()
        {
            return new ErpDbContext(_options);
        }

        public Task<ErpDbContext> CreateDbContextAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CreateDbContext());
        }

        public ValueTask DisposeAsync()
        {
            if (Directory.Exists(_directory))
            {
                Directory.Delete(_directory, recursive: true);
            }

            return ValueTask.CompletedTask;
        }
    }
}
