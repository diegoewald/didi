using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Users;
using LocalErp.Domain.Users;

namespace LocalErp.Application.Security;

public sealed class AuthenticationService(
    IUserRepository repository,
    IPasswordHashService passwordHashService,
    IUserSession session)
{
    private readonly IUserRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IPasswordHashService _passwordHashService = passwordHashService
        ?? throw new ArgumentNullException(nameof(passwordHashService));
    private readonly IUserSession _session = session
        ?? throw new ArgumentNullException(nameof(session));

    public async Task<AuthenticationResult> SignInAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var normalizedUsername = User.NormalizeUsername(username);
        var user = await _repository.GetByNormalizedUsernameAsync(normalizedUsername, cancellationToken);

        if (user is null || !user.IsActive)
        {
            return AuthenticationResult.Failed;
        }

        var verification = _passwordHashService.Verify(user.PasswordHash, password);
        if (verification == PasswordVerificationStatus.Failed)
        {
            return AuthenticationResult.Failed;
        }

        if (verification == PasswordVerificationStatus.SuccessRehashNeeded)
        {
            user.ChangePasswordHash(_passwordHashService.Hash(password));
            await _repository.UpdateAsync(user, cancellationToken);
        }

        var safeUser = UserResult.FromDomain(user);
        _session.SignIn(safeUser);
        return AuthenticationResult.Success(safeUser);
    }

    public void SignOut()
    {
        _session.SignOut();
    }
}
