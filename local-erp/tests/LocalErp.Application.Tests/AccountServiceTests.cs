using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Account;
using LocalErp.Application.Security;
using LocalErp.Application.Users;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class AccountServiceTests
{
    private const string CurrentPassword = "current-password-123";
    private const string NewPassword = "new-password-value-456";

    [TestMethod]
    public async Task ChangePasswordAsyncChangesAuthenticatedUsersPasswordAndAudits()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("diego", "hash::current");
        user.RecordFailedSignIn(
            new DateTime(2026, 10, 18, 0, 0, 0, DateTimeKind.Utc),
            TimeSpan.FromMinutes(15));
        repository.Users.Add(user);
        var session = CreateSession(user);
        var hashService = new RecordingPasswordHashService
        {
            VerificationResult = PasswordVerificationStatus.Success,
        };
        var credentialStore = new InMemoryUserCredentialStore(repository);
        var clock = new TestClock();
        var service = new AccountService(repository, credentialStore, hashService, session, clock);

        await service.ChangePasswordAsync(CurrentPassword, NewPassword);

        Assert.AreEqual($"hashed::{NewPassword}", user.PasswordHash);
        Assert.AreEqual(0, user.FailedSignInCount);
        Assert.IsNull(user.SignInBlockedUntilUtc);
        Assert.AreEqual(CurrentPassword, hashService.LastVerifiedPassword);
        Assert.AreEqual(NewPassword, hashService.LastHashedPassword);

        var auditEvent = credentialStore.Events.Single();
        Assert.AreEqual(SecurityAuditCodes.AccountPasswordChanged, auditEvent.EventCode);
        Assert.AreEqual(user.Id, auditEvent.UserId);
        Assert.AreEqual(user.Id, auditEvent.ActorUserId);
        Assert.IsNull(auditEvent.RoleId);
        Assert.IsNull(auditEvent.DetailCode);
        Assert.AreEqual(clock.UtcNow, auditEvent.OccurredAtUtc);
    }

    [TestMethod]
    public async Task ChangePasswordAsyncRejectsInvalidCurrentPasswordWithoutMutation()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("diego", "hash::current");
        repository.Users.Add(user);
        var hashService = new RecordingPasswordHashService
        {
            VerificationResult = PasswordVerificationStatus.Failed,
        };
        var credentialStore = new InMemoryUserCredentialStore(repository);
        var service = new AccountService(
            repository,
            credentialStore,
            hashService,
            CreateSession(user),
            new TestClock());

        await Assert.ThrowsExactlyAsync<CurrentPasswordInvalidException>(
            () => service.ChangePasswordAsync("wrong-current-password", NewPassword));

        Assert.AreEqual("hash::current", user.PasswordHash);
        Assert.AreEqual(0, hashService.HashCount);
        Assert.AreEqual(0, credentialStore.Events.Count);
    }

    [TestMethod]
    public async Task ChangePasswordAsyncValidatesNewPasswordBeforeVerification()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("diego", "hash::current");
        repository.Users.Add(user);
        var hashService = new RecordingPasswordHashService();
        var credentialStore = new InMemoryUserCredentialStore(repository);
        var service = new AccountService(
            repository,
            credentialStore,
            hashService,
            CreateSession(user),
            new TestClock());

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            () => service.ChangePasswordAsync(CurrentPassword, "too-short"));

        Assert.AreEqual(0, hashService.VerifyCount);
        Assert.AreEqual(0, hashService.HashCount);
        Assert.AreEqual(0, credentialStore.Events.Count);
    }

    [TestMethod]
    public async Task ChangePasswordAsyncRequiresAuthenticatedSession()
    {
        var repository = new InMemoryUserRepository();
        var session = new TestUserSession();
        var hashService = new RecordingPasswordHashService();
        var credentialStore = new InMemoryUserCredentialStore(repository);
        var service = new AccountService(
            repository,
            credentialStore,
            hashService,
            session,
            new TestClock());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.ChangePasswordAsync(CurrentPassword, NewPassword));

        Assert.AreEqual(0, repository.GetByIdCount);
        Assert.AreEqual(0, hashService.VerifyCount);
        Assert.AreEqual(0, credentialStore.Events.Count);
    }

    [TestMethod]
    public async Task ChangePasswordAsyncRejectsCredentialChangedBeforeCommit()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("diego", "hash::current");
        repository.Users.Add(user);
        var hashService = new RecordingPasswordHashService
        {
            VerificationResult = PasswordVerificationStatus.SuccessRehashNeeded,
        };
        var credentialStore = new InMemoryUserCredentialStore(repository)
        {
            RejectNextUpdate = true,
        };
        var service = new AccountService(
            repository,
            credentialStore,
            hashService,
            CreateSession(user),
            new TestClock());

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.ChangePasswordAsync(CurrentPassword, NewPassword));

        Assert.AreEqual("hash::current", user.PasswordHash);
        Assert.AreEqual(1, hashService.VerifyCount);
        Assert.AreEqual(1, hashService.HashCount);
        Assert.AreEqual(0, credentialStore.Events.Count);
    }

    private static TestUserSession CreateSession(User user)
    {
        var session = new TestUserSession();
        session.SignIn(UserResult.FromDomain(user));
        return session;
    }

    private sealed class TestClock : IClock
    {
        public DateTime UtcNow { get; } = new(2026, 10, 18, 0, 30, 0, DateTimeKind.Utc);
    }

    private sealed class RecordingPasswordHashService : IPasswordHashService
    {
        public PasswordVerificationStatus VerificationResult { get; set; } = PasswordVerificationStatus.Success;

        public int VerifyCount { get; private set; }

        public int HashCount { get; private set; }

        public string? LastVerifiedPassword { get; private set; }

        public string? LastHashedPassword { get; private set; }

        public string Hash(string password)
        {
            HashCount++;
            LastHashedPassword = password;
            return $"hashed::{password}";
        }

        public PasswordVerificationStatus Verify(string passwordHash, string providedPassword)
        {
            VerifyCount++;
            LastVerifiedPassword = providedPassword;
            return VerificationResult;
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

    private sealed class InMemoryUserCredentialStore(
        InMemoryUserRepository repository) : IUserCredentialStore
    {
        private readonly InMemoryUserRepository _repository = repository;

        public bool RejectNextUpdate { get; set; }

        public List<SecurityAuditEvent> Events { get; } = [];

        public Task<bool> TryChangePasswordAsync(
            Guid userId,
            string expectedPasswordHash,
            string newPasswordHash,
            SecurityAuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var user = _repository.Users.SingleOrDefault(item => item.Id == userId);
            if (RejectNextUpdate ||
                user is null ||
                !user.IsActive ||
                !string.Equals(user.PasswordHash, expectedPasswordHash, StringComparison.Ordinal))
            {
                RejectNextUpdate = false;
                return Task.FromResult(false);
            }

            user.ChangePasswordHash(newPasswordHash);
            user.ResetSignInFailures();
            Events.Add(auditEvent);
            return Task.FromResult(true);
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];

        public int GetByIdCount { get; private set; }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            GetByIdCount++;
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
            IReadOnlyList<User> result = Users.Where(user => includeInactive || user.IsActive).ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
            => Task.CompletedTask;
    }
}
