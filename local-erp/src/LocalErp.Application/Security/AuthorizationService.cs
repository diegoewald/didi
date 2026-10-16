using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Security;

namespace LocalErp.Application.Security;

public sealed class AuthorizationService(
    IUserSession session,
    IAuthorizationRepository repository) : IAuthorizationService
{
    private readonly IUserSession _session = session
        ?? throw new ArgumentNullException(nameof(session));
    private readonly IAuthorizationRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    public async Task<bool> IsAllowedAsync(
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedPermission = PermissionCatalog.Validate(permissionCode);
        var user = _session.CurrentUser;
        if (user is null || !user.IsActive)
        {
            return false;
        }

        return await _repository.UserHasPermissionAsync(
            user.Id,
            normalizedPermission,
            cancellationToken);
    }

    public async Task DemandAsync(
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        var normalizedPermission = PermissionCatalog.Validate(permissionCode);
        if (!await IsAllowedAsync(normalizedPermission, cancellationToken))
        {
            throw AccessDeniedException.ForPermission(normalizedPermission);
        }
    }
}
