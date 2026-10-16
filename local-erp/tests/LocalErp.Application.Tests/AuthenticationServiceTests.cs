using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Security;
using LocalErp.Application.Users;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class AuthenticationServiceTests
{
    private static readonly DateTime UtcNow = new(2026, 10, 16, 13, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task SignInAsyncAuthenticatesActiveUserAndStartsSession()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Diego", "stored-hash");
        repository.Users.Add(user);
        var hashService = new StubPasswordHashService(PasswordVerificationStatus.Success);
        var session = new RecordingUserSession();
        var attempts = new InMemoryAuthenticationAttemptStore(repository);
        var timing = new RecordingTimingProtector();
        var audit = new RecordingSecurityAuditStore();
        var service = CreateService(repository, hashService, attempts, timing, session, audit);

        var result = await service.SignInAsync("  diego  ", "correct-password");

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.User);
        Assert.AreEqual(user.Id, result.User.Id);
        Assert.AreEqual(user.Id, session.CurrentUser?.Id);
        Assert.IsTrue(session.IsAuthenticated);
        Assert.AreEqual("stored-hash", hashService.LastPasswordHash);
        Assert.AreEqual("correct-password", hashService.LastProvidedPassword);
        Assert.AreEqual(1, attempts.SuccessCalls);
        Assert.AreEqual(0, timing.Calls);
    }

    [TestMethod]
    public async Task SignInAsyncRecordsFailureForWrongPassword()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Diego", "stored-hash");
        repository.Users.Add(user);
        var attempts = new InMemoryAuthenticationAttemptStore(repository);
        var service = CreateService(
            repository,
            new StubPasswordHashService(PasswordVerificationStatus.Failed),
            attempts,
            new RecordingTimingProtector(),
            new RecordingUserSession());

        var result = await service.SignInAsync("Diego", "wrong-password");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(1, attempts.FailureCalls);
        Assert.AreEqual(1, user.FailedSignInCount);
    }

    [TestMethod]
    public async Task SignInAsyncUsesDummyVerificationForMissingUser()
    {
        var repository = new InMemoryUserRepository();
        var hashService = new StubPasswordHashService(PasswordVerificationStatus.Success);
        var timing = new RecordingTimingProtector();
        var session = new RecordingUserSession();
        var audit = new RecordingSecurityAuditStore();
        var service = CreateService(
            repository,
            hashService,
            new InMemoryAuthenticationAttemptStore(repository),
            timing,
            session,
            audit);

        var result = await service.SignInAsync("unknown", "any-password");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(0, hashService.VerifyCalls);
        Assert.AreEqual(1, timing.Calls);
        Assert.IsFalse(session.IsAuthenticated);
        var auditEvent = AssertAuditEvent(
            audit,
            SecurityAuditCodes.AuthenticationSignInFailed,
            SecurityAuditDetailCodes.UnknownAccount);
        Assert.IsNull(auditEvent.UserId);
    }

    [TestMethod]
    public async Task SignInAsyncUsesDummyVerificationForInactiveUser()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Diego", "stored-hash");
        user.Deactivate();
        repository.Users.Add(user);
        var hashService = new StubPasswordHashService(PasswordVerificationStatus.Success);
        var timing = new RecordingTimingProtector();
        var audit = new RecordingSecurityAuditStore();
        var service = CreateService(
            repository,
            hashService,
            new InMemoryAuthenticationAttemptStore(repository),
            timing,
            new RecordingUserSession(),
            audit);

        var result = await service.SignInAsync("Diego", "correct-password");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(0, hashService.VerifyCalls);
        Assert.AreEqual(1, timing.Calls);
        var auditEvent = AssertAuditEvent(
            audit,
            SecurityAuditCodes.AuthenticationSignInFailed,
            SecurityAuditDetailCodes.InactiveAccount);
        Assert.AreEqual(user.Id, auditEvent.UserId);
    }

    [TestMethod]
    public async Task SignInAsyncUsesDummyVerificationForBlockedUser()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Diego", "stored-hash");
        user.RecordFailedSignIn(UtcNow, TimeSpan.FromMinutes(15));
        repository.Users.Add(user);
        var hashService = new StubPasswordHashService(PasswordVerificationStatus.Success);
        var timing = new RecordingTimingProtector();
        var audit = new RecordingSecurityAuditStore();
        var service = CreateService(
            repository,
            hashService,
            new InMemoryAuthenticationAttemptStore(repository),
            timing,
            new RecordingUserSession(),
            audit);

        var result = await service.SignInAsync("Diego", "correct-password");

        Assert.IsFalse(result.Succeeded);
        Assert.AreEqual(0, hashService.VerifyCalls);
        Assert.AreEqual(1, timing.Calls);
        var auditEvent = AssertAuditEvent(
            audit,
            SecurityAuditCodes.AuthenticationSignInBlocked,
            SecurityAuditDetailCodes.TemporarilyBlocked);
        Assert.AreEqual(user.Id, auditEvent.UserId);
    }

    [TestMethod]
    public async Task SignInAsyncSuccessfulLoginResetsFailureState()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Diego", "stored-hash");
        user.RecordFailedSignIn(UtcNow, TimeSpan.Zero);
        user.RecordFailedSignIn(UtcNow, TimeSpan.Zero);
        repository.Users.Add(user);
        var attempts = new InMemoryAuthenticationAttemptStore(repository);
        var service = CreateService(
            repository,
            new StubPasswordHashService(PasswordVerificationStatus.Success),
            attempts,
            new RecordingTimingProtector(),
            new RecordingUserSession());

        var result = await service.SignInAsync("Diego", "correct-password");

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual(0, user.FailedSignInCount);
        Assert.IsNull(user.SignInBlockedUntilUtc);
    }

    [TestMethod]
    public async Task SignInAsyncRehashesPasswordInsideSuccessfulAttemptCompletion()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Diego", "old-hash");
        repository.Users.Add(user);
        var hashService = new StubPasswordHashService(PasswordVerificationStatus.SuccessRehashNeeded)
        {
            HashResult = "upgraded-hash",
        };
        var attempts = new InMemoryAuthenticationAttemptStore(repository);
        var service = CreateService(
            repository,
            hashService,
            attempts,
            new RecordingTimingProtector(),
            new RecordingUserSession());

        var result = await service.SignInAsync("Diego", "correct-password");

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("upgraded-hash", user.PasswordHash);
        Assert.AreEqual("upgraded-hash", attempts.LastUpgradedPasswordHash);
        Assert.AreEqual(1, hashService.HashCalls);
        Assert.AreEqual(0, repository.UpdateCalls);
    }

    [TestMethod]
    public async Task SignOutClearsAuthenticatedSession()
    {
        var repository = new InMemoryUserRepository();
        repository.Users.Add(new User("Diego", "stored-hash"));
        var session = new RecordingUserSession();
        var audit = new RecordingSecurityAuditStore();
        var service = CreateService(
            repository,
            new StubPasswordHashService(PasswordVerificationStatus.Success),
            new InMemoryAuthenticationAttemptStore(repository),
            new RecordingTimingProtector(),
            session,
            audit);

        Assert.IsTrue((await service.SignInAsync("Diego", "correct-password")).Succeeded);
        audit.Events.Clear();

        await service.SignOutAsync();

        Assert.IsFalse(session.IsAuthenticated);
        Assert.IsNull(session.CurrentUser);
        var auditEvent = AssertAuditEvent(audit, SecurityAuditCodes.AuthenticationSignOut, detailCode: null);
        Assert.AreEqual(repository.Users.Single().Id, auditEvent.UserId);
    }

    [TestMethod]
    public void ThrottlePolicyUsesProgressiveDelayAndFifteenMinuteLockout()
    {
        Assert.AreEqual(TimeSpan.Zero, AuthenticationThrottlePolicy.GetBlockDurationAfterFailure(1));
        Assert.AreEqual(TimeSpan.Zero, AuthenticationThrottlePolicy.GetBlockDurationAfterFailure(2));
        Assert.AreEqual(TimeSpan.FromSeconds(2), AuthenticationThrottlePolicy.GetBlockDurationAfterFailure(3));
        Assert.AreEqual(TimeSpan.FromSeconds(10), AuthenticationThrottlePolicy.GetBlockDurationAfterFailure(4));
        Assert.AreEqual(TimeSpan.FromMinutes(15), AuthenticationThrottlePolicy.GetBlockDurationAfterFailure(5));
        Assert.AreEqual(TimeSpan.FromMinutes(15), AuthenticationThrottlePolicy.GetBlockDurationAfterFailure(8));
    }

    private static AuthenticationService CreateService(
        InMemoryUserRepository repository,
        IPasswordHashService hashService,
        IAuthenticationAttemptStore attemptStore,
        IAuthenticationTimingProtector timingProtector,
        IUserSession session,
        RecordingSecurityAuditStore? auditStore = null)
    {
        return new AuthenticationService(
            repository,
            hashService,
            attemptStore,
            timingProtector,
            auditStore ?? new RecordingSecurityAuditStore(),
            session,
            new FixedClock(UtcNow));
    }

    private static SecurityAuditEvent AssertAuditEvent(
        RecordingSecurityAuditStore auditStore,
        string eventCode,
        string? detailCode)
    {
        Assert.AreEqual(1, auditStore.Events.Count);
        var auditEvent = auditStore.Events.Single();
        Assert.AreEqual(eventCode, auditEvent.EventCode);
        Assert.AreEqual(detailCode, auditEvent.DetailCode);
        Assert.AreEqual(UtcNow, auditEvent.OccurredAtUtc);
        return auditEvent;
    }

    private sealed class FixedClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class RecordingTimingProtector : IAuthenticationTimingProtector
    {
        public int Calls { get; private set; }

        public void PerformDummyVerification(string providedPassword)
        {
            Calls++;
        }
    }

    private sealed class StubPasswordHashService(PasswordVerificationStatus verificationStatus)
        : IPasswordHashService
    {
        public int VerifyCalls { get; private set; }
        public int HashCalls { get; private set; }
        public string? LastPasswordHash { get; private set; }
        public string? LastProvidedPassword { get; private set; }
        public string HashResult { get; init; } = "new-hash";

        public string Hash(string password)
        {
            HashCalls++;
            return HashResult;
        }

        public PasswordVerificationStatus Verify(string passwordHash, string providedPassword)
        {
            VerifyCalls++;
            LastPasswordHash = passwordHash;
            LastProvidedPassword = providedPassword;
            return verificationStatus;
        }
    }

    private sealed class RecordingUserSession : IUserSession
    {
        public bool IsAuthenticated => CurrentUser is not null;
        public UserResult? CurrentUser { get; private set; }

        public void SignIn(UserResult user) => CurrentUser = user;
        public void SignOut() => CurrentUser = null;
    }

    private sealed class RecordingSecurityAuditStore : ISecurityAuditStore
    {
        public List<SecurityAuditEvent> Events { get; } = [];

        public Task AppendAsync(
            SecurityAuditEvent auditEvent,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Events.Add(auditEvent);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryAuthenticationAttemptStore(InMemoryUserRepository repository)
        : IAuthenticationAttemptStore
    {
        public int FailureCalls { get; private set; }
        public int SuccessCalls { get; private set; }
        public string? LastUpgradedPasswordHash { get; private set; }

        public Task RecordFailedAttemptAsync(
            Guid userId,
            DateTime utcNow,
            CancellationToken cancellationToken = default)
        {
            var user = repository.Users.Single(item => item.Id == userId);
            if (user.IsSignInBlocked(utcNow))
            {
                return Task.CompletedTask;
            }

            FailureCalls++;
            var nextCount = user.FailedSignInCount + 1;
            user.RecordFailedSignIn(
                utcNow,
                AuthenticationThrottlePolicy.GetBlockDurationAfterFailure(nextCount));
            return Task.CompletedTask;
        }

        public Task<bool> CompleteSuccessfulAttemptAsync(
            Guid userId,
            DateTime utcNow,
            string? upgradedPasswordHash,
            CancellationToken cancellationToken = default)
        {
            SuccessCalls++;
            var user = repository.Users.Single(item => item.Id == userId);
            if (!user.IsActive || user.IsSignInBlocked(utcNow))
            {
                return Task.FromResult(false);
            }

            LastUpgradedPasswordHash = upgradedPasswordHash;
            if (!string.IsNullOrWhiteSpace(upgradedPasswordHash))
            {
                user.ChangePasswordHash(upgradedPasswordHash);
            }

            user.ResetSignInFailures();
            return Task.FromResult(true);
        }
    }

    private sealed class InMemoryUserRepository : IUserRepository
    {
        public List<User> Users { get; } = [];
        public int UpdateCalls { get; private set; }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            Users.Add(user);
            return Task.CompletedTask;
        }

        public Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.SingleOrDefault(user => user.Id == id));

        public Task<User?> GetByNormalizedUsernameAsync(
            string normalizedUsername,
            CancellationToken cancellationToken = default) =>
            Task.FromResult(Users.SingleOrDefault(user => user.NormalizedUsername == normalizedUsername));

        public Task<IReadOnlyList<User>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            IReadOnlyList<User> result = Users
                .Where(user => includeInactive || user.IsActive)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(User user, CancellationToken cancellationToken = default)
        {
            UpdateCalls++;
            return Task.CompletedTask;
        }
    }
}
