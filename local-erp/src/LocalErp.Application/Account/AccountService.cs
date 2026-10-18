using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Security;
using LocalErp.Domain.Security;

namespace LocalErp.Application.Account;

public sealed class AccountService(
    IUserRepository userRepository,
    IUserCredentialStore credentialStore,
    IPasswordHashService passwordHashService,
    IUserSession session,
    IClock clock)
{
    private readonly IUserRepository _userRepository = userRepository
        ?? throw new ArgumentNullException(nameof(userRepository));
    private readonly IUserCredentialStore _credentialStore = credentialStore
        ?? throw new ArgumentNullException(nameof(credentialStore));
    private readonly IPasswordHashService _passwordHashService = passwordHashService
        ?? throw new ArgumentNullException(nameof(passwordHashService));
    private readonly IUserSession _session = session
        ?? throw new ArgumentNullException(nameof(session));
    private readonly IClock _clock = clock
        ?? throw new ArgumentNullException(nameof(clock));

    public async Task ChangePasswordAsync(
        string currentPassword,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(currentPassword);
        PasswordPolicy.Validate(newPassword);

        var sessionUser = _session.CurrentUser
            ?? throw new InvalidOperationException("Changing the current password requires an authenticated session.");

        var persistedUser = await _userRepository.GetByIdAsync(sessionUser.Id, cancellationToken)
            ?? throw new InvalidOperationException("The authenticated user no longer exists.");
        if (!persistedUser.IsActive)
        {
            throw new InvalidOperationException("The authenticated user is no longer active.");
        }

        var verification = _passwordHashService.Verify(persistedUser.PasswordHash, currentPassword);
        if (verification == PasswordVerificationStatus.Failed)
        {
            throw new CurrentPasswordInvalidException();
        }

        var newPasswordHash = _passwordHashService.Hash(newPassword);
        var auditEvent = new SecurityAuditEvent(
            _clock.UtcNow,
            SecurityAuditCodes.AccountPasswordChanged,
            userId: persistedUser.Id,
            actorUserId: persistedUser.Id);

        var changed = await _credentialStore.TryChangePasswordAsync(
            persistedUser.Id,
            persistedUser.PasswordHash,
            newPasswordHash,
            auditEvent,
            cancellationToken);
        if (!changed)
        {
            throw new InvalidOperationException(
                "The account or its credential changed before the password update could be committed.");
        }
    }
}
