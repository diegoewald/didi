namespace LocalErp.Application.Security;

public sealed class StartupFlowService(
    AdministratorBootstrapService administratorBootstrapService,
    IUserSession session)
{
    private readonly AdministratorBootstrapService _administratorBootstrapService =
        administratorBootstrapService ?? throw new ArgumentNullException(nameof(administratorBootstrapService));
    private readonly IUserSession _session = session
        ?? throw new ArgumentNullException(nameof(session));

    public async Task<StartupRoute> DetermineRouteAsync(
        CancellationToken cancellationToken = default)
    {
        if (await _administratorBootstrapService.IsRequiredAsync(cancellationToken))
        {
            return StartupRoute.AdministratorBootstrapRequired;
        }

        return _session.IsAuthenticated
            ? StartupRoute.Main
            : StartupRoute.SignInRequired;
    }
}
