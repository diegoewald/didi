using System.Data;
using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Security;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class SqliteUserCredentialStore(
    IDbContextFactory<ErpDbContext> contextFactory) : IUserCredentialStore
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task<bool> TryChangePasswordAsync(
        Guid userId,
        string expectedPasswordHash,
        string newPasswordHash,
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        ValidateArguments(userId, expectedPasswordHash, newPasswordHash, auditEvent);

        await using var context = await CreateImmediateTransactionalContextAsync(cancellationToken);
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("User credential transaction was not created.");

        try
        {
            var user = await context.Users.SingleOrDefaultAsync(
                entry => entry.Id == userId,
                cancellationToken);
            if (user is null ||
                !user.IsActive ||
                !string.Equals(user.PasswordHash, expectedPasswordHash, StringComparison.Ordinal))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            user.ChangePasswordHash(newPasswordHash);
            user.ResetSignInFailures();
            context.SecurityAuditEvents.Add(auditEvent);
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

    private static void ValidateArguments(
        Guid userId,
        string expectedPasswordHash,
        string newPasswordHash,
        SecurityAuditEvent auditEvent)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        ArgumentException.ThrowIfNullOrWhiteSpace(expectedPasswordHash);
        ArgumentException.ThrowIfNullOrWhiteSpace(newPasswordHash);
        ArgumentNullException.ThrowIfNull(auditEvent);

        if (auditEvent.EventCode != SecurityAuditCodes.AccountPasswordChanged ||
            auditEvent.UserId != userId ||
            auditEvent.ActorUserId != userId ||
            auditEvent.RoleId.HasValue ||
            auditEvent.DetailCode is not null)
        {
            throw new ArgumentException(
                "Self-service password audit context does not match the affected account.",
                nameof(auditEvent));
        }
    }

    private async Task<ErpDbContext> CreateImmediateTransactionalContextAsync(
        CancellationToken cancellationToken)
    {
        var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);

        if (context.Database.GetDbConnection() is not SqliteConnection connection)
        {
            await context.DisposeAsync();
            throw new InvalidOperationException(
                "User credential persistence requires a Microsoft.Data.Sqlite connection.");
        }

        var transaction = connection.BeginTransaction(
            IsolationLevel.Serializable,
            deferred: false);
        context.Database.UseTransaction(transaction);
        return context;
    }
}
