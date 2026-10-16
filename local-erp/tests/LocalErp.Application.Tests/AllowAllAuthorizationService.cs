using LocalErp.Application.Security;

namespace LocalErp.Application.Tests;

internal sealed class AllowAllAuthorizationService : IAuthorizationService
{
    public Task<bool> IsAllowedAsync(
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(true);
    }

    public Task DemandAsync(
        string permissionCode,
        CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }
}
