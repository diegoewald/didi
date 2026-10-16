using LocalErp.Domain.Users;

namespace LocalErp.Application.Abstractions.Persistence;

public interface IUserRepository
{
    Task AddAsync(User user, CancellationToken cancellationToken = default);

    Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<User?> GetByNormalizedUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<User>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(User user, CancellationToken cancellationToken = default);
}
