using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Users;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class EfUserRepository(IDbContextFactory<ErpDbContext> contextFactory) : IUserRepository
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Users.Add(user);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<User?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(user => user.Id == id, cancellationToken);
    }

    public async Task<User?> GetByNormalizedUsernameAsync(
        string normalizedUsername,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedUsername);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Users
            .AsNoTracking()
            .SingleOrDefaultAsync(
                user => user.NormalizedUsername == normalizedUsername,
                cancellationToken);
    }

    public async Task<IReadOnlyList<User>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<User> query = context.Users.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(user => user.IsActive);
        }

        return await query
            .OrderBy(user => user.Username)
            .ThenBy(user => user.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Users.Update(user);
        await context.SaveChangesAsync(cancellationToken);
    }
}
