using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class EfPurchaseRequestRepository(IDbContextFactory<ErpDbContext> contextFactory)
    : IPurchaseRequestRepository
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task AddAsync(PurchaseRequest purchaseRequest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(purchaseRequest);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.PurchaseRequests.Add(purchaseRequest);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<PurchaseRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.PurchaseRequests
            .AsNoTracking()
            .Include(request => request.Lines)
            .SingleOrDefaultAsync(request => request.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseRequest>> ListAsync(
        PurchaseRequestStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<PurchaseRequest> query = context.PurchaseRequests
            .AsNoTracking()
            .Include(request => request.Lines);

        if (status is PurchaseRequestStatus requestedStatus)
        {
            query = query.Where(request => request.Status == requestedStatus);
        }

        return await query
            .OrderByDescending(request => request.CreatedAtUtc)
            .ThenBy(request => request.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(PurchaseRequest purchaseRequest, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(purchaseRequest);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var persisted = await context.PurchaseRequests
            .Include(request => request.Lines)
            .SingleOrDefaultAsync(request => request.Id == purchaseRequest.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase request '{purchaseRequest.Id}' was not found.");

        context.Entry(persisted).CurrentValues.SetValues(purchaseRequest);

        var incomingById = purchaseRequest.Lines.ToDictionary(line => line.Id);
        foreach (var persistedLine in persisted.Lines.ToArray())
        {
            if (!incomingById.TryGetValue(persistedLine.Id, out var incomingLine))
            {
                context.PurchaseRequestLines.Remove(persistedLine);
                continue;
            }

            context.Entry(persistedLine).CurrentValues.SetValues(incomingLine);
            incomingById.Remove(persistedLine.Id);
        }

        foreach (var newLine in incomingById.Values)
        {
            context.PurchaseRequestLines.Add(newLine);
        }

        await context.SaveChangesAsync(cancellationToken);
    }
}
