using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Application.Users;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class StartupFlowServiceTests
{
    [TestMethod]
    public async Task DetermineRouteAsyncRequiresBootstrapBeforeAnyOtherRoute()
    {
        var session = new StubUserSession();
        session.SignIn(new UserResult(Guid.NewGuid(), "admin", true));
        var service = CreateService(bootstrapRequired: true, session);

        var route = await service.DetermineRouteAsync();

        Assert.AreEqual(StartupRoute.AdministratorBootstrapRequired, route);
    }

    [TestMethod]
    public async Task DetermineRouteAsyncRequiresSignInAfterBootstrapWhenSessionIsEmpty()
    {
        var service = CreateService(bootstrapRequired: false, new StubUserSession());

        var route = await service.DetermineRouteAsync();

        Assert.AreEqual(StartupRoute.SignInRequired, route);
    }

    [TestMethod]
    public async Task DetermineRouteAsyncOpensMainOnlyForAuthenticatedSession()
    {
        var session = new StubUserSession();
        session.SignIn(new UserResult(Guid.NewGuid(), "admin", true));
        var service = CreateService(bootstrapRequired: false, session);

        var route = await service.DetermineRouteAsync();

        Assert.AreEqual(StartupRoute.Main, route);
    }

    private static StartupFlowService CreateService(bool bootstrapRequired, IUserSession session)
    {
        var bootstrap = new AdministratorBootstrapService(
            new StubBootstrapStore(bootstrapRequired),
            new StubPasswordHashService());
        return new StartupFlowService(bootstrap, session);
    }

    private sealed class StubBootstrapStore(bool bootstrapRequired) : IAdministratorBootstrapStore
    {
        public Task<bool> IsBootstrapRequiredAsync(CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(bootstrapRequired);
        }

        public Task<bool> TryBootstrapAsync(
            User administrator,
            Role administratorRole,
            IReadOnlyCollection<string> permissionCodes,
            CancellationToken cancellationToken = default)
        {
            throw new NotSupportedException();
        }
    }

    private sealed class StubPasswordHashService : IPasswordHashService
    {
        public string Hash(string password) => "hash";

        public PasswordVerificationStatus Verify(string passwordHash, string providedPassword)
        {
            return PasswordVerificationStatus.Success;
        }
    }

    private sealed class StubUserSession : IUserSession
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
