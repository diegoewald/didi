using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Users;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;

namespace LocalErp.Application.Security;

public sealed class AuthenticationService(
    IUserRepository repository,
    IPasswordHashService passwordHashService,
    IAuthenticationAttemptStore attemptStore,
    IAuthenticationTimingProtector timingProtector,
    ISecurityAuditStore auditStore,
    IUserSession session,
    IClock clock)
{
    private readonly IUserRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IPasswordHashService _passwordHashService = passwordHashService
        ?? throw new ArgumentNullException(nameof(passwordHashService));
    private readonly IAuthenticationAttemptStore _attemptStore = attemptStore
        ?? throw new ArgumentNullException(nameof(attemptStore));
    private readonly IAuthenticationTimingProtector _timingProtector = timingProtector
        ?? throw new ArgumentNullException(nameof(timingProtector));
    private readonly ISecurityAuditStore _auditStore = auditStore
        ?? throw new ArgumentNullException(nameof(auditStore));
    private readonly IUserSession _session = session
        ?? throw new ArgumentNullException(nameof(session));
    private readonly IClock _clock = clock
        ?? throw new ArgumentNullException(nameof(clock));

    public async Task<AuthenticationResult> SignInAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var normalizedUsername = User.NormalizeUsername(username);
        var user = await _repository.GetByNormalizedUsernameAsync(normalizedUsername, cancellationToken);
        var utcNow = _clock.UtcNow;

        if (user is null)
        {
            _timingProtector.PerformDummyVerification(password);
            await _auditStore.AppendAsync(
                new SecurityAuditEvent(
                    utcNow,
                    SecurityAuditCodes.AuthenticationSignInFailed,
                    detailCode: SecurityAuditDetailCodes.UnknownAccount),
                cancellationToken);
            return AuthenticationResult.Failed;
        }

        if (!user.IsActive)
        {
            _timingProtector.PerformDummyVerification(password);
            await _auditStore.AppendAsync(
                new SecurityAuditEvent(
                    utcNow,
                    SecurityAuditCodes.AuthenticationSignInFailed,
                    user.Id,
                    SecurityAuditDetailCodes.InactiveAccount),
                cancellationToken);
            return AuthenticationResult.Failed;
        }

        if (user.IsSignInBlocked(utcNow))
        {
            _timingProtector.PerformDummyVerification(password);
            await _auditStore.AppendAsync(
                new SecurityAuditEvent(
                    utcNow,
                    SecurityAuditCodes.AuthenticationSignInBlocked,
                    user.Id,
                    SecurityAuditDetailCodes.TemporarilyBlocked),
                cancellationToken);
            return AuthenticationResult.Failed;
        }

        var verification = _passwordHashService.Verify(user.PasswordHash, password);
        if (verification == PasswordVerificationStatus.Failed)
        {
            await _attemptStore.RecordFailedAttemptAsync(user.Id, utcNow, cancellationToken);
            return AuthenticationResult.Failed;
        }

        var upgradedPasswordHash = verification == PasswordVerificationStatus.SuccessRehashNeeded
            ? _passwordHashService.Hash(password)
            : null;

        var completed = await _attemptStore.CompleteSuccessfulAttemptAsync(
            user.Id,
            utcNow,
            upgradedPasswordHash,
            cancellationToken);

        if (!completed)
        {
            return AuthenticationResult.Failed;
        }

        if (upgradedPasswordHash is not null)
        {
            user.ChangePasswordHash(upgradedPasswordHash);
        }

        user.ResetSignInFailures();
        var safeUser = UserResult.FromDomain(user);
        _session.SignIn(safeUser);
        return AuthenticationResult.Success(safeUser);
    }

    public async Task SignOutAsync(CancellationToken cancellationToken = default)
    {
        var user = _session.CurrentUser;
        _session.SignOut();

        if (user is null)
        {
            return;
        }

        await _auditStore.AppendAsync(
            new SecurityAuditEvent(
                _clock.UtcNow,
                SecurityAuditCodes.AuthenticationSignOut,
                user.Id),
            cancellationToken);
    }
}
