using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Security;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class EfSecurityAuditStore(
    IDbContextFactory<ErpDbContext> contextFactory) : ISecurityAuditStore
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task AppendAsync(
        SecurityAuditEvent auditEvent,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(auditEvent);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.SecurityAuditEvents.Add(auditEvent);
        await context.SaveChangesAsync(cancellationToken);
    }
}
