using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Roles;
using LocalErp.Application.Users;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;

namespace LocalErp.Application.Security;

public sealed class AdministratorBootstrapService(
    IAdministratorBootstrapStore store,
    IPasswordHashService passwordHashService)
{
    public const string AdministratorRoleName = "Administrator";

    private readonly IAdministratorBootstrapStore _store = store
        ?? throw new ArgumentNullException(nameof(store));
    private readonly IPasswordHashService _passwordHashService = passwordHashService
        ?? throw new ArgumentNullException(nameof(passwordHashService));

    public Task<bool> IsRequiredAsync(CancellationToken cancellationToken = default)
    {
        return _store.IsBootstrapRequiredAsync(cancellationToken);
    }

    public async Task<AdministratorBootstrapResult> BootstrapAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        PasswordPolicy.Validate(password);

        if (!await _store.IsBootstrapRequiredAsync(cancellationToken))
        {
            throw new InvalidOperationException("Administrator bootstrap has already been completed.");
        }

        var administrator = new User(username, _passwordHashService.Hash(password));
        var administratorRole = new Role(AdministratorRoleName);
        var permissionCodes = PermissionCatalog.All
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        var committed = await _store.TryBootstrapAsync(
            administrator,
            administratorRole,
            permissionCodes,
            cancellationToken);

        if (!committed)
        {
            throw new InvalidOperationException(
                "Administrator bootstrap was completed by another initialization attempt.");
        }

        return new AdministratorBootstrapResult(
            UserResult.FromDomain(administrator),
            RoleResult.FromDomain(administratorRole, permissionCodes));
    }
}
