using LocalErp.Application.Security;

namespace LocalErp.Infrastructure.Security;

public sealed class PasswordHashTimingProtector : IAuthenticationTimingProtector
{
    private const string WorkFactorProbePassword = "LocalErp authentication timing probe v1";

    private readonly IPasswordHashService _passwordHashService;
    private readonly string _dummyPasswordHash;

    public PasswordHashTimingProtector(IPasswordHashService passwordHashService)
    {
        _passwordHashService = passwordHashService
            ?? throw new ArgumentNullException(nameof(passwordHashService));
        _dummyPasswordHash = _passwordHashService.Hash(WorkFactorProbePassword);
    }

    public void PerformDummyVerification(string providedPassword)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(providedPassword);
        _ = _passwordHashService.Verify(_dummyPasswordHash, providedPassword);
    }
}
