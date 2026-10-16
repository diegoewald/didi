using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Application.Users;
using LocalErp.Domain.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class AuthorizationServiceTests
{
    [TestMethod]
    public async Task IsAllowedAsyncDeniesUnauthenticatedSessionWithoutRepositoryLookup()
    {
        var repository = new RecordingAuthorizationRepository(allowed: true);
        var service = new AuthorizationService(new TestUserSession(), repository);

        var allowed = await service.IsAllowedAsync(PermissionCatalog.CompaniesRead);

        Assert.IsFalse(allowed);
        Assert.AreEqual(0, repository.Calls);
    }

    [TestMethod]
    public async Task IsAllowedAsyncUsesAuthenticatedUserAndRepository()
    {
        var user = new UserResult(Guid.NewGuid(), "operator", true);
        var session = new TestUserSession();
        session.SignIn(user);
        var repository = new RecordingAuthorizationRepository(allowed: true);
        var service = new AuthorizationService(session, repository);

        var allowed = await service.IsAllowedAsync(PermissionCatalog.CompaniesRead);

        Assert.IsTrue(allowed);
        Assert.AreEqual(user.Id, repository.LastUserId);
        Assert.AreEqual(PermissionCatalog.CompaniesRead, repository.LastPermissionCode);
    }

    [TestMethod]
    public async Task DemandAsyncThrowsAccessDeniedWhenPermissionIsMissing()
    {
        var session = new TestUserSession();
        session.SignIn(new UserResult(Guid.NewGuid(), "operator", true));
        var service = new AuthorizationService(
            session,
            new RecordingAuthorizationRepository(allowed: false));

        var exception = await Assert.ThrowsExactlyAsync<AccessDeniedException>(
            () => service.DemandAsync(PermissionCatalog.UsersManage));

        Assert.AreEqual(PermissionCatalog.UsersManage, exception.PermissionCode);
    }

    private sealed class RecordingAuthorizationRepository(bool allowed) : IAuthorizationRepository
    {
        public int Calls { get; private set; }

        public Guid LastUserId { get; private set; }

        public string? LastPermissionCode { get; private set; }

        public Task<bool> UserHasPermissionAsync(
            Guid userId,
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            Calls++;
            LastUserId = userId;
            LastPermissionCode = permissionCode;
            return Task.FromResult(allowed);
        }
    }

    private sealed class TestUserSession : IUserSession
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
}
