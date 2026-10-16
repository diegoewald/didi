using System.Data;
using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Domain.Security;
using LocalErp.Domain.Users;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class SqliteAdministratorBootstrapStore(
    IDbContextFactory<ErpDbContext> contextFactory,
    IClock clock) : IAdministratorBootstrapStore
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));
    private readonly IClock _clock = clock
        ?? throw new ArgumentNullException(nameof(clock));

    public async Task<bool> IsBootstrapRequiredAsync(
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return !await context.Users.AsNoTracking().AnyAsync(cancellationToken);
    }

    public async Task<bool> TryBootstrapAsync(
        User administrator,
        Role administratorRole,
        IReadOnlyCollection<string> permissionCodes,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(administrator);
        ArgumentNullException.ThrowIfNull(administratorRole);
        ArgumentNullException.ThrowIfNull(permissionCodes);

        var validatedPermissions = permissionCodes
            .Select(PermissionCatalog.Validate)
            .Distinct(StringComparer.Ordinal)
            .OrderBy(code => code, StringComparer.Ordinal)
            .ToArray();

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);

        if (context.Database.GetDbConnection() is not SqliteConnection connection)
        {
            throw new InvalidOperationException(
                "Administrator bootstrap requires a Microsoft.Data.Sqlite connection.");
        }

        await using var transaction = connection.BeginTransaction(
            IsolationLevel.Serializable,
            deferred: false);

        context.Database.UseTransaction(transaction);

        try
        {
            if (await context.Users.AsNoTracking().AnyAsync(cancellationToken))
            {
                await transaction.RollbackAsync(cancellationToken);
                return false;
            }

            context.Users.Add(administrator);
            context.Roles.Add(administratorRole);
            context.UserRoles.Add(new UserRole(administrator.Id, administratorRole.Id));
            context.RolePermissions.AddRange(
                validatedPermissions.Select(permissionCode =>
                    new RolePermissionGrant(administratorRole.Id, permissionCode)));
            context.SecurityAuditEvents.Add(
                new SecurityAuditEvent(
                    _clock.UtcNow,
                    SecurityAuditCodes.AdministratorBootstrapSucceeded,
                    administrator.Id));

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
}
