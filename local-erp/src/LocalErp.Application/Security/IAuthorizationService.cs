namespace LocalErp.Application.Security;

public interface IAuthorizationService
{
    Task<bool> IsAllowedAsync(
        string permissionCode,
        CancellationToken cancellationToken = default);

    Task DemandAsync(
        string permissionCode,
        CancellationToken cancellationToken = default);
}
