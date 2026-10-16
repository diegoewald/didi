using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Domain.Users;
using LocalErp.Domain.Security;

namespace LocalErp.Application.Users;

public sealed class UserService(
    IUserRepository repository,
    IPasswordHashService passwordHashService,
    IAuthorizationService authorizationService)
{
    private readonly IUserRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IPasswordHashService _passwordHashService = passwordHashService
        ?? throw new ArgumentNullException(nameof(passwordHashService));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));

    public async Task<UserResult> CreateAsync(
        string username,
        string password,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.UsersManage, cancellationToken);
        ArgumentException.ThrowIfNullOrWhiteSpace(password);

        var normalizedUsername = User.NormalizeUsername(username);
        var existing = await _repository.GetByNormalizedUsernameAsync(normalizedUsername, cancellationToken);
        if (existing is not null)
        {
            throw new InvalidOperationException("A user with the same username already exists.");
        }

        var user = new User(username, _passwordHashService.Hash(password));
        await _repository.AddAsync(user, cancellationToken);
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
        ArgumentException.ThrowIfNullOrWhiteSpace(newPassword);

        var user = await GetRequiredAsync(userId, cancellationToken);
        user.ChangePasswordHash(_passwordHashService.Hash(newPassword));
        await _repository.UpdateAsync(user, cancellationToken);
        return UserResult.FromDomain(user);
    }

    public async Task<UserResult> SetActiveAsync(
        Guid userId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.UsersManage, cancellationToken);
        var user = await GetRequiredAsync(userId, cancellationToken);

        if (isActive)
        {
            user.Activate();
        }
        else
        {
            user.Deactivate();
        }

        await _repository.UpdateAsync(user, cancellationToken);
        return UserResult.FromDomain(user);
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
