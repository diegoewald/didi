using System.Data;
using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class SqliteAuthenticationAttemptStore(
    IDbContextFactory<ErpDbContext> contextFactory) : IAuthenticationAttemptStore
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task RecordFailedAttemptAsync(
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return;
        }

        await using var context = await CreateTransactionalContextAsync(cancellationToken);
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Authentication attempt transaction was not created.");

        try
        {
            var user = await context.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
            if (user is null || !user.IsActive || user.IsSignInBlocked(utcNow))
            {
                await transaction.RollbackAsync(cancellationToken);
                return;
            }

            var nextFailureCount = checked(user.FailedSignInCount + 1);
            var blockDuration = AuthenticationThrottlePolicy.GetBlockDurationAfterFailure(nextFailureCount);
            user.RecordFailedSignIn(utcNow, blockDuration);

            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<bool> CompleteSuccessfulAttemptAsync(
        Guid userId,
        DateTime utcNow,
        string? upgradedPasswordHash,
        CancellationToken cancellationToken = default)
    {
        if (userId == Guid.Empty)
        {
            return false;
        }

        await using var context = await CreateTransactionalContextAsync(cancellationToken);
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Authentication attempt transaction was not created.");

        try
        {
            var user = await context.Users.SingleOrDefaultAsync(item => item.Id == userId, cancellationToken);
            if (user is null || !user.IsActive || user.IsSignInBlocked(utcNow))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            if (!string.IsNullOrWhiteSpace(upgradedPasswordHash))
            {
                user.ChangePasswordHash(upgradedPasswordHash);
            }

            user.ResetSignInFailures();
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return true;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    private async Task<ErpDbContext> CreateTransactionalContextAsync(
        CancellationToken cancellationToken)
    {
        var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);

        if (context.Database.GetDbConnection() is not SqliteConnection connection)
        {
            await context.DisposeAsync();
            throw new InvalidOperationException(
                "Authentication attempt persistence requires a Microsoft.Data.Sqlite connection.");
        }

        var transaction = connection.BeginTransaction(
            IsolationLevel.Serializable,
            deferred: false);
        context.Database.UseTransaction(transaction);
        return context;
    }
}
