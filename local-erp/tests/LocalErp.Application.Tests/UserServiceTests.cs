using LocalErp.Application.Abstractions.Persistence;
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
        var service = new UserService(repository, hashService, new AllowAllAuthorizationService());

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
        var service = new UserService(repository, new RecordingPasswordHashService(), new AllowAllAuthorizationService());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.CreateAsync("  DIEGO  ", AnotherValidPassword));
    }

    [TestMethod]
    public async Task CreateAsyncDemandsUsersManagePermission()
    {
        var repository = new InMemoryUserRepository();
        var authorization = new RecordingAuthorizationService();
        var service = new UserService(repository, new RecordingPasswordHashService(), authorization);

        await service.CreateAsync("diego", ValidPassword);

        Assert.AreEqual(PermissionCatalog.UsersManage, authorization.LastDemandedPermission);
    }

    [TestMethod]
    public async Task CreateAsyncRejectsShortPasswordBeforeHashing()
    {
        var repository = new InMemoryUserRepository();
        var hashService = new RecordingPasswordHashService();
        var service = new UserService(repository, hashService, new AllowAllAuthorizationService());

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            () => service.CreateAsync("diego", "too-short"));

        Assert.IsNull(hashService.LastPassword);
        Assert.AreEqual(0, repository.Users.Count);
    }

    [TestMethod]
    public async Task ChangePasswordAsyncReplacesOnlyStoredHash()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("diego", "old-hash");
        repository.Users.Add(user);
        var hashService = new RecordingPasswordHashService();
        var service = new UserService(repository, hashService, new AllowAllAuthorizationService());

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
        var service = new UserService(repository, hashService, new AllowAllAuthorizationService());

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            () => service.ChangePasswordAsync(user.Id, "too-short"));

        Assert.IsNull(hashService.LastPassword);
        Assert.AreEqual("old-hash", user.PasswordHash);
    }

    [TestMethod]
    public async Task ListAsyncExcludesInactiveUsersByDefault()
    {
        var repository = new InMemoryUserRepository();
        repository.Users.Add(new User("active", "hash"));
        var inactive = new User("inactive", "hash");
        inactive.Deactivate();
        repository.Users.Add(inactive);
        var service = new UserService(repository, new RecordingPasswordHashService(), new AllowAllAuthorizationService());

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
        var service = new UserService(repository, new RecordingPasswordHashService(), new AllowAllAuthorizationService());

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
        var service = new UserService(repository, new RecordingPasswordHashService(), authorization);

        await service.ListAsync(includeInactive: true);

        Assert.AreEqual(PermissionCatalog.UsersRead, authorization.LastDemandedPermission);
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

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

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
            return Task.CompletedTask;
        }
    }
}
