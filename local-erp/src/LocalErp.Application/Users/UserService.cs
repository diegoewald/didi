using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Security;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;

namespace LocalErp.Application.Users;

public sealed class UserService(
    IUserRepository repository,
    IPasswordHashService passwordHashService,
    IAuthorizationService authorizationService,
    IUserSession session,
    IAdministrativeAuditStore administrativeAuditStore,
    IClock clock)
{
    private readonly IUserRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IPasswordHashService _passwordHashService = passwordHashService
        ?? throw new ArgumentNullException(nameof(passwordHashService));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));
    private readonly IUserSession _session = session
        ?? throw new ArgumentNullException(nameof(session));
    private readonly IAdministrativeAuditStore _administrativeAuditStore = administrativeAuditStore
        ?? throw new ArgumentNullException(nameof(administrativeAuditStore));
    private readonly IClock _clock = clock ?? throw new ArgumentNullException(nameof(clock));

    public async Task<UserResult> CreateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.UsersManage, cancellationToken);
        PasswordPolicy.Validate(password);

        var normalizedUsername = User.NormalizeUsername(username);
        var existing = await _repository.GetByNormalizedUsernameAsync(normalizedUsername, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("A user with the same username already exists.");
        }

        var user = new User(username, _passwordHashService.Hash(password));
        var auditEvent = CreateAdministrativeAuditEvent(SecurityAuditCodes.AdministrationUserCreated, user.Id);
        await _administrativeAuditStore.AddUserAsync(user, auditEvent, cancellationToken);
        return UserResult.FromDomain(user);
    }

    public async Task<IReadOnlyList<UserResult>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.UsersRead, cancellationToken);
        var users = await _repository.ListAsync(includeInactive, cancellationToken);
        return users.Select(UserResult.FromDomain).ToArray();
    }

    public async Task<UserResult> ChangePasswordAsync(
        Guid userId,
        string newPassword,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.UsersManage, cancellationToken);
        PasswordPolicy.Validate(newPassword);

        var user = await GetRequiredAsync(userId, cancellationToken);
        var passwordHash = _passwordHashService.Hash(newPassword);
        user.ChangePasswordHash(passwordHash);
        user.ResetSignInFailures();
        var auditEvent = CreateAdministrativeAuditEvent(
            SecurityAuditCodes.AdministrationUserPasswordReset,
            user.Id);
        await _administrativeAuditStore.UpdateUserAsync(user, auditEvent, cancellationToken);
        return UserResult.FromDomain(user);
    }

    public async Task<UserResult> SetActiveAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.UsersManage, cancellationToken);
        var user = await GetRequiredAsync(userId, cancellationToken);

        if (user.IsActive == isActive)
        {
            return UserResult.FromDomain(user);
        }

        if (!isActive && _session.CurrentUser?.Id == user.Id)
        {
            throw new SelfDeactivationNotAllowedException();
        }

        if (isActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        var auditEvent = CreateAdministrativeAuditEvent(
            isActive
                ? SecurityAuditCodes.AdministrationUserActivated
                : SecurityAuditCodes.AdministrationUserDeactivated,
            user.Id);
        await _administrativeAuditStore.UpdateUserAsync(user, auditEvent, cancellationToken);
        return UserResult.FromDomain(user);
    }

    private SecurityAuditEvent CreateAdministrativeAuditEvent(string eventCode, Guid targetUserId)
    {
        var actor = _session.CurrentUser
            ?? throw new InvalidOperationException("Administrative operations require an authenticated user.");

        return new SecurityAuditEvent(
            _clock.UtcNow,
            eventCode,
            userId: targetUserId,
            actorUserId: actor.Id);
    }

    private async Task<User> GetRequiredAsync(Guid userId, CancellationToken cancellationToken)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        return await _repository.GetByIdAsync(userId, cancellationToken)
            ?? throw new KeyNotFoundException($"User '{userId}' was not found.");
    }
}
