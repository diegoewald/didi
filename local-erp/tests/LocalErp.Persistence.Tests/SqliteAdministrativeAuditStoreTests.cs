using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using LocalErp.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class SqliteAdministrativeAuditStoreTests
{
    private static readonly DateTime AuditTime = new(2026, 10, 17, 23, 45, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task AddUserAsyncPersistsUserAndAuditEventTogether()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var store = new SqliteAdministrativeAuditStore(database);
        var actor = new User("admin", "hash");
        await users.AddAsync(actor);
        var target = new User("new-user", "hash");
        var auditEvent = new SecurityAuditEvent(
            AuditTime,
            SecurityAuditCodes.AdministrationUserCreated,
            userId: target.Id,
            actorUserId: actor.Id);

        await store.AddUserAsync(target, auditEvent);

        await using var context = database.CreateDbContext();
        Assert.AreEqual(2, await context.Users.CountAsync());
        var persistedAudit = await context.SecurityAuditEvents.SingleAsync();
        Assert.AreEqual(target.Id, persistedAudit.UserId);
        Assert.AreEqual(actor.Id, persistedAudit.ActorUserId);
        Assert.AreEqual(SecurityAuditCodes.AdministrationUserCreated, persistedAudit.EventCode);
    }

    [TestMethod]
    public async Task UpdateUserAsyncRollsBackUserChangeWhenAuditForeignKeyFails()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var store = new SqliteAdministrativeAuditStore(database);
        var target = new User("target", "hash");
        await users.AddAsync(target);
        var detachedTarget = await users.GetByIdAsync(target.Id)
            ?? throw new InvalidOperationException("Test user was not persisted.");
        detachedTarget.Deactivate();
        var auditEvent = new SecurityAuditEvent(
            AuditTime,
            SecurityAuditCodes.AdministrationUserDeactivated,
            userId: target.Id,
            actorUserId: Guid.NewGuid());

        await Assert.ThrowsExactlyAsync<DbUpdateException>(
            () => store.UpdateUserAsync(detachedTarget, auditEvent));

        var reloaded = await users.GetByIdAsync(target.Id)
            ?? throw new InvalidOperationException("Test user disappeared after rollback.");
        Assert.IsTrue(reloaded.IsActive);
        await using var context = database.CreateDbContext();
        Assert.AreEqual(0, await context.SecurityAuditEvents.CountAsync());
    }

    [TestMethod]
    public async Task AssignUserRoleAsyncRollsBackAssignmentWhenAuditForeignKeyFails()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var roles = new EfRoleRepository(database);
        var store = new SqliteAdministrativeAuditStore(database);
        var user = new User("operator", "hash");
        var role = new Role("Operators");
        await users.AddAsync(user);
        await roles.AddAsync(role);
        var auditEvent = new SecurityAuditEvent(
            AuditTime,
            SecurityAuditCodes.AdministrationUserRoleAssigned,
            userId: user.Id,
            actorUserId: Guid.NewGuid(),
            roleId: role.Id);

        await Assert.ThrowsExactlyAsync<DbUpdateException>(
            () => store.AssignUserRoleAsync(user.Id, role.Id, auditEvent));

        await using var context = database.CreateDbContext();
        Assert.AreEqual(0, await context.UserRoles.CountAsync());
        Assert.AreEqual(0, await context.SecurityAuditEvents.CountAsync());
    }

    [TestMethod]
    public async Task ConcurrentAdministrativeRoleRemovalsPreserveOneManagerAndAuditOneRemoval()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var roles = new EfRoleRepository(database);
        var store = new SqliteAdministrativeAuditStore(database);

        var firstUser = new User("admin-one", "hash");
        var secondUser = new User("admin-two", "hash");
        var role = new Role("Administrators");
        await users.AddAsync(firstUser);
        await users.AddAsync(secondUser);
        await roles.AddAsync(role);
        await roles.GrantPermissionAsync(role.Id, PermissionCatalog.RolesManage);
        await roles.AssignUserAsync(firstUser.Id, role.Id);
        await roles.AssignUserAsync(secondUser.Id, role.Id);

        var firstAudit = new SecurityAuditEvent(
            AuditTime,
            SecurityAuditCodes.AdministrationUserRoleRemoved,
            userId: firstUser.Id,
            actorUserId: firstUser.Id,
            roleId: role.Id);
        var secondAudit = new SecurityAuditEvent(
            AuditTime,
            SecurityAuditCodes.AdministrationUserRoleRemoved,
            userId: secondUser.Id,
            actorUserId: secondUser.Id,
            roleId: role.Id);

        var results = await Task.WhenAll(
            store.RemoveUserRolePreservingPermissionHolderAsync(
                firstUser.Id,
                role.Id,
                PermissionCatalog.RolesManage,
                firstAudit),
            store.RemoveUserRolePreservingPermissionHolderAsync(
                secondUser.Id,
                role.Id,
                PermissionCatalog.RolesManage,
                secondAudit));

        Assert.AreEqual(1, results.Count(result => result == RoleAssignmentRemovalResult.Removed));
        Assert.AreEqual(
            1,
            results.Count(result => result == RoleAssignmentRemovalResult.WouldRemoveLastPermissionHolder));

        await using var context = database.CreateDbContext();
        Assert.AreEqual(1, await context.UserRoles.CountAsync());
        Assert.AreEqual(1, await context.SecurityAuditEvents.CountAsync());
        Assert.AreEqual(
            SecurityAuditCodes.AdministrationUserRoleRemoved,
            (await context.SecurityAuditEvents.SingleAsync()).EventCode);
    }

    [TestMethod]
    public async Task AddRoleAsyncRollsBackRoleWhenAuditActorForeignKeyFails()
    {
        await using var database = await TestDatabase.CreateAsync();
        var store = new SqliteAdministrativeAuditStore(database);
        var role = new Role("Operators");
        var auditEvent = new SecurityAuditEvent(
            AuditTime,
            SecurityAuditCodes.AdministrationRoleCreated,
            actorUserId: Guid.NewGuid(),
            roleId: role.Id);

        await Assert.ThrowsExactlyAsync<DbUpdateException>(
            () => store.AddRoleAsync(role, auditEvent));

        await using var context = database.CreateDbContext();
        Assert.AreEqual(0, await context.Roles.CountAsync());
        Assert.AreEqual(0, await context.SecurityAuditEvents.CountAsync());
    }

    [TestMethod]
    public async Task DeactivateRoleBlocksLossOfLastEffectiveManagerWithoutAudit()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var roles = new EfRoleRepository(database);
        var store = new SqliteAdministrativeAuditStore(database);
        var actor = new User("admin", "hash");
        var role = new Role("Administrators");
        await users.AddAsync(actor);
        await roles.AddAsync(role);
        await roles.GrantPermissionAsync(role.Id, PermissionCatalog.RolesManage);
        await roles.AssignUserAsync(actor.Id, role.Id);
        var auditEvent = new SecurityAuditEvent(
            AuditTime,
            SecurityAuditCodes.AdministrationRoleDeactivated,
            actorUserId: actor.Id,
            roleId: role.Id);

        var result = await store.SetRoleActivePreservingPermissionHolderAsync(
            role.Id,
            false,
            PermissionCatalog.RolesManage,
            auditEvent);

        Assert.AreEqual(ProtectedRoleMutationResult.WouldRemoveLastPermissionHolder, result);
        var reloaded = await roles.GetByIdAsync(role.Id)
            ?? throw new InvalidOperationException("Role disappeared after protected mutation.");
        Assert.IsTrue(reloaded.IsActive);
        await using var context = database.CreateDbContext();
        Assert.AreEqual(0, await context.SecurityAuditEvents.CountAsync());
    }

    [TestMethod]
    public async Task RevokeRolesManageSucceedsWhenAnotherEffectiveManagerRemainsAndAuditsPermission()
    {
        await using var database = await TestDatabase.CreateAsync();
        var users = new EfUserRepository(database);
        var roles = new EfRoleRepository(database);
        var store = new SqliteAdministrativeAuditStore(database);
        var firstUser = new User("admin-one", "hash");
        var secondUser = new User("admin-two", "hash");
        var firstRole = new Role("Administrators A");
        var secondRole = new Role("Administrators B");
        await users.AddAsync(firstUser);
        await users.AddAsync(secondUser);
        await roles.AddAsync(firstRole);
        await roles.AddAsync(secondRole);
        await roles.GrantPermissionAsync(firstRole.Id, PermissionCatalog.RolesManage);
        await roles.GrantPermissionAsync(secondRole.Id, PermissionCatalog.RolesManage);
        await roles.AssignUserAsync(firstUser.Id, firstRole.Id);
        await roles.AssignUserAsync(secondUser.Id, secondRole.Id);
        var auditEvent = new SecurityAuditEvent(
            AuditTime,
            SecurityAuditCodes.AdministrationRolePermissionRevoked,
            detailCode: PermissionCatalog.RolesManage,
            actorUserId: firstUser.Id,
            roleId: firstRole.Id);

        var result = await store.RevokeRolePermissionPreservingPermissionHolderAsync(
            firstRole.Id,
            PermissionCatalog.RolesManage,
            PermissionCatalog.RolesManage,
            auditEvent);

        Assert.AreEqual(ProtectedRoleMutationResult.Changed, result);
        CollectionAssert.DoesNotContain(
            (System.Collections.ICollection)(await roles.GetPermissionsAsync(firstRole.Id)),
            PermissionCatalog.RolesManage);
        CollectionAssert.Contains(
            (System.Collections.ICollection)(await roles.GetPermissionsAsync(secondRole.Id)),
            PermissionCatalog.RolesManage);
        await using var context = database.CreateDbContext();
        var persistedAudit = await context.SecurityAuditEvents.SingleAsync();
        Assert.AreEqual(SecurityAuditCodes.AdministrationRolePermissionRevoked, persistedAudit.EventCode);
        Assert.AreEqual(PermissionCatalog.RolesManage, persistedAudit.DetailCode);
        Assert.AreEqual(firstRole.Id, persistedAudit.RoleId);
        Assert.IsNull(persistedAudit.UserId);
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
            var databasePath = Path.Combine(directory, "administrative-audit.db");
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
