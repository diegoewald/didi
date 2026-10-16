using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Application.Users;
using LocalErp.Domain.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class UserServiceTests
{
    [TestMethod]
    public async Task CreateAsyncHashesPasswordAndReturnsSafeProjection()
    {
        var repository = new InMemoryUserRepository();
        var hashService = new RecordingPasswordHashService();
        var service = new UserService(repository, hashService, new AllowAllAuthorizationService());

        var result = await service.CreateAsync(" Diego ", "secret-value");

        Assert.AreEqual("Diego", result.Username);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual("secret-value", hashService.LastPassword);
        Assert.AreEqual("hashed::secret-value", repository.Users.Single().PasswordHash);
    }

    [TestMethod]
    public async Task CreateAsyncRejectsDuplicateNormalizedUsername()
    {
        var repository = new InMemoryUserRepository();
        repository.Users.Add(new User("Diego", "existing-hash"));
        var service = new UserService(repository, new RecordingPasswordHashService(), new AllowAllAuthorizationService());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.CreateAsync("  DIEGO  ", "another-secret"));
    }

    [TestMethod]
    public async Task ChangePasswordAsyncReplacesOnlyStoredHash()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("diego", "old-hash");
        repository.Users.Add(user);
        var hashService = new RecordingPasswordHashService();
        var service = new UserService(repository, hashService, new AllowAllAuthorizationService());

        var result = await service.ChangePasswordAsync(user.Id, "new-secret");

        Assert.AreEqual(user.Id, result.Id);
        Assert.AreEqual("hashed::new-secret", repository.Users.Single().PasswordHash);
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
