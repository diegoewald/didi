namespace LocalErp.Application.Abstractions.Persistence;

public interface IAuthorizationRepository
{
    Task<bool> UserHasPermissionAsync(
        Guid userId,
        string permissionCode,
        CancellationToken cancellationToken = default);
}
