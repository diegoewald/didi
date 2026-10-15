using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Application.Users;
using LocalErp.Domain.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class AuthenticationServiceTests
{
    [TestMethod]
    public async Task SignInAsyncAuthenticatesActiveUserAndStartsSession()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Diego", "stored-hash");
        repository.Users.Add(user);
        var hashService = new StubPasswordHashService(PasswordVerificationStatus.Success);
        var session = new RecordingUserSession();
        var service = new AuthenticationService(repository, hashService, session);

        var result = await service.SignInAsync("  diego  ", "correct-password");

        Assert.IsTrue(result.Succeeded);
        Assert.IsNotNull(result.User);
        Assert.AreEqual(user.Id, result.User.Id);
        Assert.AreEqual(user.Id, session.CurrentUser?.Id);
        Assert.IsTrue(session.IsAuthenticated);
        Assert.AreEqual("stored-hash", hashService.LastPasswordHash);
        Assert.AreEqual("correct-password", hashService.LastProvidedPassword);
    }

    [TestMethod]
    public async Task SignInAsyncRejectsWrongPasswordWithoutStartingSession()
    {
        var repository = new InMemoryUserRepository();
        repository.Users.Add(new User("Diego", "stored-hash"));
        var session = new RecordingUserSession();
        var service = new AuthenticationService(
            repository,
            new StubPasswordHashService(PasswordVerificationStatus.Failed),
            session);

        var result = await service.SignInAsync("Diego", "wrong-password");

        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(result.User);
        Assert.IsFalse(session.IsAuthenticated);
        Assert.AreEqual(0, repository.UpdateCalls);
    }

    [TestMethod]
    public async Task SignInAsyncRejectsMissingUserWithoutVerifyingPassword()
    {
        var repository = new InMemoryUserRepository();
        var hashService = new StubPasswordHashService(PasswordVerificationStatus.Success);
        var session = new RecordingUserSession();
        var service = new AuthenticationService(repository, hashService, session);

        var result = await service.SignInAsync("unknown", "any-password");

        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(result.User);
        Assert.AreEqual(0, hashService.VerifyCalls);
        Assert.IsFalse(session.IsAuthenticated);
    }

    [TestMethod]
    public async Task SignInAsyncRejectsInactiveUserWithoutVerifyingPassword()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Diego", "stored-hash");
        user.Deactivate();
        repository.Users.Add(user);
        var hashService = new StubPasswordHashService(PasswordVerificationStatus.Success);
        var session = new RecordingUserSession();
        var service = new AuthenticationService(repository, hashService, session);

        var result = await service.SignInAsync("Diego", "correct-password");

        Assert.IsFalse(result.Succeeded);
        Assert.IsNull(result.User);
        Assert.AreEqual(0, hashService.VerifyCalls);
        Assert.IsFalse(session.IsAuthenticated);
    }

    [TestMethod]
    public async Task SignInAsyncRehashesPasswordWhenVerifierRequestsUpgrade()
    {
        var repository = new InMemoryUserRepository();
        var user = new User("Diego", "old-hash");
        repository.Users.Add(user);
        var hashService = new StubPasswordHashService(PasswordVerificationStatus.SuccessRehashNeeded)
        {
            HashResult = "upgraded-hash",
        };
        var session = new RecordingUserSession();
        var service = new AuthenticationService(repository, hashService, session);

        var result = await service.SignInAsync("Diego", "correct-password");

        Assert.IsTrue(result.Succeeded);
        Assert.AreEqual("upgraded-hash", user.PasswordHash);
        Assert.AreEqual(1, hashService.HashCalls);
        Assert.AreEqual(1, repository.UpdateCalls);
        Assert.IsTrue(session.IsAuthenticated);
    }

    [TestMethod]
    public async Task SignOutClearsAuthenticatedSession()
    {
        var repository = new InMemoryUserRepository();
        repository.Users.Add(new User("Diego", "stored-hash"));
        var session = new RecordingUserSession();
        var service = new AuthenticationService(
            repository,
            new StubPasswordHashService(PasswordVerificationStatus.Success),
            session);

        var result = await service.SignInAsync("Diego", "correct-password");
        Assert.IsTrue(result.Succeeded);

        service.SignOut();

        Assert.IsFalse(session.IsAuthenticated);
        Assert.IsNull(session.CurrentUser);
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

        public void SignIn(UserResult user)
        {
            CurrentUser = user;
        }

        public void SignOut()
        {
            CurrentUser = null;
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
