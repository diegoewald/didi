using LocalErp.Domain.Security;
using LocalErp.Domain.Users;

namespace LocalErp.Application.Abstractions.Persistence;

public interface IAdministratorBootstrapStore
{
    Task<bool> IsBootstrapRequiredAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Atomically creates the first administrator, administrator role, role permissions,
    /// and user-role assignment only when no user exists yet.
    /// </summary>
    /// <returns>
    /// <see langword="true"/> when bootstrap was committed; otherwise <see langword="false"/>
    /// when another initialization has already created a user.
    /// </returns>
    Task<bool> TryBootstrapAsync(
        User administrator,
        Role administratorRole,
        IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken = default);
}
