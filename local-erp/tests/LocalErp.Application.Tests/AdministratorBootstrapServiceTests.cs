using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class AdministratorBootstrapServiceTests
{
    [TestMethod]
    public async Task IsRequiredAsyncReturnsStoreState()
    {
        var store = new RecordingBootstrapStore { IsRequired = true };
        var service = CreateService(store);

        var required = await service.IsRequiredAsync();

        Assert.IsTrue(required);
        Assert.AreEqual(1, store.IsRequiredCalls);
        Assert.AreEqual(0, store.TryBootstrapCalls);
    }

    [TestMethod]
    public async Task BootstrapAsyncCreatesAdministratorRoleWithAllKnownPermissions()
    {
        var store = new RecordingBootstrapStore();
        var hashService = new StubPasswordHashService { HashResult = "secure-hash" };
        var service = new AdministratorBootstrapService(store, hashService);

        var result = await service.BootstrapAsync("  Admin  ", "correct horse battery staple");

        Assert.AreEqual(1, hashService.HashCalls);
        Assert.AreEqual("correct horse battery staple", hashService.LastPassword);
        Assert.AreEqual(1, store.TryBootstrapCalls);
        Assert.IsNotNull(store.Administrator);
        Assert.IsNotNull(store.AdministratorRole);
        Assert.AreEqual("Admin", store.Administrator.Username);
        Assert.AreEqual("ADMIN", store.Administrator.NormalizedUsername);
        Assert.AreEqual("secure-hash", store.Administrator.PasswordHash);
        Assert.IsTrue(store.Administrator.IsActive);
        Assert.AreEqual(AdministratorBootstrapService.AdministratorRoleName, store.AdministratorRole.Name);
        Assert.IsTrue(store.AdministratorRole.IsActive);
        CollectionAssert.AreEquivalent(
            PermissionCatalog.All.ToArray(),
            store.PermissionCodes.ToArray());

        Assert.AreEqual(store.Administrator.Id, result.Administrator.Id);
        Assert.AreEqual("Admin", result.Administrator.Username);
        Assert.AreEqual(store.AdministratorRole.Id, result.AdministratorRole.Id);
        CollectionAssert.AreEquivalent(
            PermissionCatalog.All.ToArray(),
            result.AdministratorRole.Permissions.ToArray());
    }

    [TestMethod]
    public async Task BootstrapAsyncRejectsAlreadyInitializedSystemBeforeHashing()
    {
        var store = new RecordingBootstrapStore { IsRequired = false };
        var hashService = new StubPasswordHashService();
        var service = new AdministratorBootstrapService(store, hashService);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.BootstrapAsync("Admin", "correct horse battery staple"));

        Assert.AreEqual(0, hashService.HashCalls);
        Assert.AreEqual(0, store.TryBootstrapCalls);
    }

    [TestMethod]
    public async Task BootstrapAsyncFailsClosedWhenAnotherAttemptWinsRace()
    {
        var store = new RecordingBootstrapStore { TryBootstrapResult = false };
        var service = CreateService(store);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(
            () => service.BootstrapAsync("Admin", "correct horse battery staple"));

        Assert.AreEqual(1, store.TryBootstrapCalls);
    }

    [TestMethod]
    public async Task BootstrapAsyncRejectsPasswordShorterThanMinimum()
    {
        var store = new RecordingBootstrapStore();
        var hashService = new StubPasswordHashService();
        var service = new AdministratorBootstrapService(store, hashService);
        var password = new string('x', PasswordPolicy.MinimumLength - 1);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            () => service.BootstrapAsync("Admin", password));

        Assert.AreEqual(0, store.IsRequiredCalls);
        Assert.AreEqual(0, hashService.HashCalls);
    }

    [TestMethod]
    public async Task BootstrapAsyncRejectsPasswordLongerThanMaximum()
    {
        var store = new RecordingBootstrapStore();
        var hashService = new StubPasswordHashService();
        var service = new AdministratorBootstrapService(store, hashService);
        var password = new string('x', PasswordPolicy.MaximumLength + 1);

        await Assert.ThrowsExactlyAsync<ArgumentOutOfRangeException>(
            () => service.BootstrapAsync("Admin", password));

        Assert.AreEqual(0, store.IsRequiredCalls);
        Assert.AreEqual(0, hashService.HashCalls);
    }

    [TestMethod]
    public async Task BootstrapAsyncRejectsWhitespaceOnlyPassword()
    {
        var store = new RecordingBootstrapStore();
        var hashService = new StubPasswordHashService();
        var service = new AdministratorBootstrapService(store, hashService);

        await Assert.ThrowsExactlyAsync<ArgumentException>(
            () => service.BootstrapAsync("Admin", new string(' ', PasswordPolicy.MinimumLength)));

        Assert.AreEqual(0, store.IsRequiredCalls);
        Assert.AreEqual(0, hashService.HashCalls);
    }

    private static AdministratorBootstrapService CreateService(RecordingBootstrapStore store)
    {
        return new AdministratorBootstrapService(store, new StubPasswordHashService());
    }

    private sealed class StubPasswordHashService : IPasswordHashService
    {
        public int HashCalls { get; private set; }

        public string? LastPassword { get; private set; }

        public string HashResult { get; init; } = "hashed-password";

        public string Hash(string password)
        {
            HashCalls++;
            LastPassword = password;
            return HashResult;
        }

        public PasswordVerificationStatus Verify(string passwordHash, string providedPassword)
        {
            return PasswordVerificationStatus.Failed;
        }
    }

    private sealed class RecordingBootstrapStore : IAdministratorBootstrapStore
    {
        public bool IsRequired { get; init; } = true;

        public bool TryBootstrapResult { get; init; } = true;

        public int IsRequiredCalls { get; private set; }

        public int TryBootstrapCalls { get; private set; }

        public User? Administrator { get; private set; }

        public Role? AdministratorRole { get; private set; }

        public IReadOnlyCollection<string> PermissionCodes { get; private set; } = [];

        public Task<bool> IsBootstrapRequiredAsync(CancellationToken cancellationToken = default)
        {
            IsRequiredCalls++;
            return Task.FromResult(IsRequired);
        }

        public Task<bool> TryBootstrapAsync(
            User administrator,
            Role administratorRole,
            IReadOnlyCollection<string> permissionCodes,
            CancellationToken cancellationToken = default)
        {
            TryBootstrapCalls++;
            Administrator = administrator;
            AdministratorRole = administratorRole;
            PermissionCodes = permissionCodes.ToArray();
            return Task.FromResult(TryBootstrapResult);
        }
    }
}
