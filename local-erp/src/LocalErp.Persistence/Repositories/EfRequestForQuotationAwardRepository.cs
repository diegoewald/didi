using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class EfRequestForQuotationAwardRepository(IDbContextFactory<ErpDbContext> contextFactory)
    : IRequestForQuotationAwardRepository
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task AddAsync(RequestForQuotationAward award, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(award);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.RequestForQuotationAwards.Add(award);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RequestForQuotationAward?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await FullQuery(context)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<RequestForQuotationAward?> GetByRequestForQuotationIdAsync(
        Guid requestForQuotationId,
        CancellationToken cancellationToken = default)
    {
        if (requestForQuotationId == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await FullQuery(context)
            .SingleOrDefaultAsync(
                item => item.RequestForQuotationId == requestForQuotationId,
                cancellationToken);
    }

    public async Task UpdateAsync(RequestForQuotationAward award, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(award);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var persisted = await context.RequestForQuotationAwards
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Id == award.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"RFQ award decision '{award.Id}' was not found.");

        context.Entry(persisted).CurrentValues.SetValues(award);
        SyncLines(context, persisted, award);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<RequestForQuotationAward> FullQuery(ErpDbContext context) =>
        context.RequestForQuotationAwards
            .AsNoTracking()
            .Include(item => item.Lines);

    private static void SyncLines(
        ErpDbContext context,
        RequestForQuotationAward persisted,
        RequestForQuotationAward incoming)
    {
        var incomingById = incoming.Lines.ToDictionary(line => line.Id);
        foreach (var persistedLine in persisted.Lines.ToArray())
        {
            if (!incomingById.TryGetValue(persistedLine.Id, out var incomingLine))
            {
                context.RequestForQuotationAwardLines.Remove(persistedLine);
                continue;
            }

            context.Entry(persistedLine).CurrentValues.SetValues(incomingLine);
            incomingById.Remove(persistedLine.Id);
        }

        foreach (var newLine in incomingById.Values)
        {
            context.RequestForQuotationAwardLines.Add(newLine);
        }
    }
}
