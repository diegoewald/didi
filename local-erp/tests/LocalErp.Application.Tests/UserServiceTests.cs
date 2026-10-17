using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Security;
using LocalErp.Application.Users;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class UserServiceTests
{
    private const string ValidPassword = "secret-value-1234";
    private const string AnotherValidPassword = "another-secret-1234";
    private const string NewValidPassword = "new-secret-value-123";

    [TestMethod]
    public async Task CreateAsyncHashesPasswordAndReturnsSafeProjection()
    {
        var repository = new InMemoryUserRepository();
        var hashService = new RecordingPasswordHashService();
        var service = CreateService(repository, hashService);

        var result = await service.CreateAsync(" Diego ", ValidPassword);

        Assert.AreEqual("Diego", result.Username);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual(ValidPassword, hashService.LastPassword);
        Assert.AreEqual($"hashed::{ValidPassword}", repository.Users.Single().PasswordHash);
    }

    [TestMethod]
    public async Task CreateAsyncRejectsDuplicateNormalizedUsername()
    {
        var repository = new InMemoryUserRepository();
        repository.Users.Add(new User("Diego", "existing-hash"));
        var service = CreateService(repository);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.CreateAsync("  DIEGO  ", AnotherValidPassword));
    }

    [TestMethod]
    public async Task CreateAsyncDemandsUsersManagePermission()
    {
        var repository = new InMemoryUserRepository();
        var authorization = new RecordingAuthorizationService();
        var service = CreateService(repository, authorizationService: authorization);

        await service.CreateAsync("diego", ValidPassword);

        Assert.AreEqual(PermissionCatalog.UsersManage, authorization.LastDemandedPermission);
    }

    [TestMethod]
    public async Task CreateAsyncRejectsShortPasswordBeforeHashing()
    {
        var repository = new InMemoryUserRepository();
        var hashService = new RecordingPasswordHashService();
        var service = CreateService(repository, hashService);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            () => service.CreateAsync("diego", "too-short"));

        Assert.IsNull(hashService.LastPassword);
        Assert.AreEqual(0, repository.Users.Count);
    }

    [TestMethod]
    public async Task CreateAsyncAuditsActorAndCreatedUser()
    {
        var repository = new InMemoryUserRepository();
        var session = CreateAuthenticatedSession();
        var auditStore = new InMemoryAdministrativeAuditStore(repository);
        var clock = new TestClock();
        var service = CreateService(
            repository,
            session: session,
            administrativeAuditStore: auditStore,
            clock: clock);

        var result = await service.CreateAsync("audited-user", ValidPassword);

        var auditEvent = auditStore.Events.Single();
        Assert.AreEqual(SecurityAuditCodes.AdministrationUserCreated, auditEvent.EventCode);
        Assert.AreEqual(result.Id, auditEvent.UserId);
        Assert.AreEqual(session.CurrentUser!.Id, auditEvent.ActorUserId);
        Assert.IsNull(auditEvent.RoleId);
        Assert.AreEqual(clock.UtcNow, auditEvent.OccurredAtUtc);
    }

    [TestMethod]
    public async Task ChangePasswordAsyncReplacesOnlyStoredHash()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("diego", "old-hash");
        repository.Users.Add(user);
        var hashService = new RecordingPasswordHashService();
        var service = CreateService(repository, hashService);

        var result = await service.ChangePasswordAsync(user.Id, NewValidPassword);

        Assert.AreEqual(user.Id, result.Id);
        Assert.AreEqual($"hashed::{NewValidPassword}", repository.Users.Single().PasswordHash);
    }

    [TestMethod]
    public async Task ChangePasswordAsyncRejectsShortPasswordBeforeHashing()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("diego", "old-hash");
        repository.Users.Add(user);
        var hashService = new RecordingPasswordHashService();
        var service = CreateService(repository, hashService);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            () => service.ChangePasswordAsync(user.Id, "too-short"));

        Assert.IsNull(hashService.LastPassword);
        Assert.AreEqual("old-hash", user.PasswordHash);
    }

    [TestMethod]
    public async Task ChangePasswordAsyncDemandsUsersManagePermission()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("target", "old-hash");
        repository.Users.Add(user);
        var authorization = new RecordingAuthorizationService();
        var service = CreateService(repository, authorizationService: authorization);

        await service.ChangePasswordAsync(user.Id, NewValidPassword);

        Assert.AreEqual(PermissionCatalog.UsersManage, authorization.LastDemandedPermission);
    }

    [TestMethod]
    public async Task ChangePasswordAsyncClearsSignInFailuresAndPreservesInactiveStatus()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("target", "old-hash");
        user.RecordFailedSignIn(
            new DateTime(2026, 10, 17, 21, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromMinutes(15));
        user.Deactivate();
        repository.Users.Add(user);
        var service = CreateService(repository);

        var result = await service.ChangePasswordAsync(user.Id, NewValidPassword);

        Assert.AreEqual(0, user.FailedSignInCount);
        Assert.IsNull(user.SignInBlockedUntilUtc);
        Assert.IsFalse(user.IsActive);
        Assert.IsFalse(result.IsActive);
        Assert.AreEqual(1, repository.UpdateCount);
    }

    [TestMethod]
    public async Task ChangePasswordAsyncDoesNotHashWhenUserDoesNotExist()
    {
        var repository = new InMemoryUserRepository();
        var hashService = new RecordingPasswordHashService();
        var service = CreateService(repository, hashService);

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
            () => service.ChangePasswordAsync(Guid.NewGuid(), NewValidPassword));

        Assert.IsNull(hashService.LastPassword);
        Assert.AreEqual(0, repository.UpdateCount);
    }

    [TestMethod]
    public async Task ChangePasswordAsyncAuditsPasswordResetWithoutPasswordData()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("target", "old-hash");
        repository.Users.Add(user);
        var session = CreateAuthenticatedSession();
        var auditStore = new InMemoryAdministrativeAuditStore(repository);
        var service = CreateService(repository, session: session, administrativeAuditStore: auditStore);

        await service.ChangePasswordAsync(user.Id, NewValidPassword);

        var auditEvent = auditStore.Events.Single();
        Assert.AreEqual(SecurityAuditCodes.AdministrationUserPasswordReset, auditEvent.EventCode);
        Assert.AreEqual(user.Id, auditEvent.UserId);
        Assert.AreEqual(session.CurrentUser!.Id, auditEvent.ActorUserId);
        Assert.IsNull(auditEvent.DetailCode);
    }

    [TestMethod]
    public async Task ListAsyncExcludesInactiveUsersByDefault()
    {
        var repository = new InMemoryUserRepository();
        repository.Users.Add(new User("active", "hash"));
        var inactive = new User("inactive", "hash");
        inactive.Deactivate();
        repository.Users.Add(inactive);
        var service = CreateService(repository);

        var result = await service.ListAsync();

        Assert.AreEqual(1, result.Count);
        Assert.AreEqual("active", result[0].Username);
    }

    [TestMethod]
    public async Task ListAsyncIncludesInactiveUsersWhenRequested()
    {
        var repository = new InMemoryUserRepository();
        repository.Users.Add(new User("active", "hash"));
        var inactive = new User("inactive", "hash");
        inactive.Deactivate();
        repository.Users.Add(inactive);
        var service = CreateService(repository);

        var result = await service.ListAsync(includeInactive: true);

        Assert.AreEqual(2, result.Count);
        Assert.IsTrue(result.Any(user => user.Username == "active" && user.IsActive));
        Assert.IsTrue(result.Any(user => user.Username == "inactive" && !user.IsActive));
    }

    [TestMethod]
    public async Task ListAsyncDemandsUsersReadPermission()
    {
        var repository = new InMemoryUserRepository();
        repository.Users.Add(new User("active", "hash"));
        var authorization = new RecordingAuthorizationService();
        var service = CreateService(repository, authorizationService: authorization);

        await service.ListAsync(includeInactive: true);

        Assert.AreEqual(PermissionCatalog.UsersRead, authorization.LastDemandedPermission);
    }

    [TestMethod]
    public async Task SetActiveAsyncDemandsUsersManagePermission()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("target", "hash");
        repository.Users.Add(user);
        var authorization = new RecordingAuthorizationService();
        var service = CreateService(repository, authorizationService: authorization);

        await service.SetActiveAsync(user.Id, false);

        Assert.AreEqual(PermissionCatalog.UsersManage, authorization.LastDemandedPermission);
        Assert.IsFalse(user.IsActive);
    }

    [TestMethod]
    public async Task SetActiveAsyncDeactivatesAnotherUser()
    {
        var repository = new InMemoryUserRepository();
        var actor = new User("actor", "hash");
        var target = new User("target", "hash");
        repository.Users.Add(actor);
        repository.Users.Add(target);
        var session = new TestUserSession();
        session.SignIn(UserResult.FromDomain(actor));
        var service = CreateService(repository, session: session);

        var result = await service.SetActiveAsync(target.Id, false);

        Assert.IsFalse(result.IsActive);
        Assert.IsFalse(target.IsActive);
        Assert.IsTrue(actor.IsActive);
        Assert.AreEqual(1, repository.UpdateCount);
    }

    [TestMethod]
    public async Task SetActiveAsyncActivatesInactiveUser()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("target", "hash");
        user.Deactivate();
        repository.Users.Add(user);
        var service = CreateService(repository);

        var result = await service.SetActiveAsync(user.Id, true);

        Assert.IsTrue(result.IsActive);
        Assert.IsTrue(user.IsActive);
        Assert.AreEqual(1, repository.UpdateCount);
    }

    [TestMethod]
    public async Task SetActiveAsyncRejectsSelfDeactivationBeforePersistence()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("actor", "hash");
        repository.Users.Add(user);
        var session = new TestUserSession();
        session.SignIn(UserResult.FromDomain(user));
        var auditStore = new InMemoryAdministrativeAuditStore(repository);
        var service = CreateService(repository, session: session, administrativeAuditStore: auditStore);

        await Assert.ThrowsExactlyAsync<SelfDeactivationNotAllowedException>(
            () => service.SetActiveAsync(user.Id, false));

        Assert.IsTrue(user.IsActive);
        Assert.AreEqual(0, repository.UpdateCount);
        Assert.AreEqual(0, auditStore.Events.Count);
    }

    [TestMethod]
    public async Task SetActiveAsyncAuditsDeactivationAndActivation()
    {
        var repository = new InMemoryUserRepository();
        var target = new User("target", "hash");
        repository.Users.Add(target);
        var auditStore = new InMemoryAdministrativeAuditStore(repository);
        var service = CreateService(repository, administrativeAuditStore: auditStore);

        await service.SetActiveAsync(target.Id, false);
        await service.SetActiveAsync(target.Id, true);

        Assert.AreEqual(2, auditStore.Events.Count);
        Assert.AreEqual(SecurityAuditCodes.AdministrationUserDeactivated, auditStore.Events[0].EventCode);
        Assert.AreEqual(SecurityAuditCodes.AdministrationUserActivated, auditStore.Events[1].EventCode);
        Assert.IsTrue(auditStore.Events.All(item => item.UserId == target.Id));
    }

    private static UserService CreateService(
        InMemoryUserRepository repository,
        IPasswordHashService? passwordHashService = null,
        IAuthorizationService? authorizationService = null,
        IUserSession? session = null,
        IAdministrativeAuditStore? administrativeAuditStore = null,
        IClock? clock = null)
    {
        return new UserService(
            repository,
            passwordHashService ?? new RecordingPasswordHashService(),
            authorizationService ?? new AllowAllAuthorizationService(),
            session ?? CreateAuthenticatedSession(),
            administrativeAuditStore ?? new InMemoryAdministrativeAuditStore(repository),
            clock ?? new TestClock());
    }

    private static TestUserSession CreateAuthenticatedSession()
    {
        var session = new TestUserSession();
        session.SignIn(new UserResult(Guid.NewGuid(), "actor", true));
        return session;
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow { get; } = new(2026, 10, 17, 23, 45, 0, DateTimeKind.Utc);
    }

    private sealed class RecordingPasswordHashService : IPasswordHashService
    {
        public string? LastPassword { get; private set; }

        public string Hash(string password)
        {
            LastPassword = password;
            return $"hashed::{password}";
        }

        public PasswordVerificationStatus Verify(string passwordHash, string providedPassword)
        {
            return passwordHash == $"hashed::{providedPassword}"
                ? PasswordVerificationStatus.Success
                : PasswordVerificationStatus.Failed;
        }
    }

    private sealed class RecordingAuthorizationService : IAuthorizationService
    {
        public string? LastDemandedPermission { get; private set; }

        public Task<bool> IsAllowedAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }

        public Task DemandAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            LastDemandedPermission = permissionCode;
            return Task.CompletedTask;
        }
    }

    private sealed class TestUserSession : IUserSession
    {
        public bool IsAuthenticated => CurrentUser is not null;

        public UserResult? CurrentUser { get; private set; }

        public void SignIn(UserResult user)
        {
            CurrentUser = user ?? throw new ArgumentNullException(nameof(user));
        }

        public void SignOut()
        {
            CurrentUser = null;
        }
    }

    private sealed class InMemoryAdministrativeAuditStore(
        InMemoryUserRepository userRepository) : IAdministrativeAuditStore
    {
        private readonly InMemoryUserRepository _userRepository = userRepository;

        public List<SecurityAuditEvent> Events { get; } = [];

        public async Task AddUserAsync(
            User user,
            SecurityAuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            await _userRepository.AddAsync(user, cancellationToken);
            Events.Add(auditEvent);
        }

        public async Task UpdateUserAsync(
            User user,
            SecurityAuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            await _userRepository.UpdateAsync(user, cancellationToken);
            Events.Add(auditEvent);
        }

        public Task<bool> AssignUserRoleAsync(
            Guid userId,
            Guid roleId,
            SecurityAuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }

        public Task<RoleAssignmentRemovalResult> RemoveUserRolePreservingPermissionHolderAsync(
            Guid userId,
            Guid roleId,
            string protectedPermissionCode,
            SecurityAuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public int UpdateCount { get; private set; }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(Users.SingleOrDefault(user => user.Id == id));
        }

        public Task<User?> GetByNormalizedUsernameAsync(
            string normalizedUsername,
            CancellationToken cancellationToken = default)
        {
            return Task.FromResult(
                Users.SingleOrDefault(user => user.NormalizedUsername == normalizedUsername));
        }

        public Task<IReadOnlyList<User>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<User> result = Users
                .Where(user => includeInactive || user.IsActive)
                .OrderBy(user => user.Username)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            UpdateCount++;
            return Task.CompletedTask;
        }
    }
}
