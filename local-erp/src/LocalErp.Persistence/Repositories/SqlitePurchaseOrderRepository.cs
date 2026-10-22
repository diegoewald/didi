using System.Data;
using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Purchasing;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class SqlitePurchaseOrderRepository(IDbContextFactory<ErpDbContext> contextFactory)
    : IPurchaseOrderRepository
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task<PurchaseOrder> AddWithNextSequenceAsync(
        Func<int, PurchaseOrder> purchaseOrderFactory,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrderFactory);

        await using var context = await CreateImmediateTransactionalContextAsync(cancellationToken);
        var transaction = context.Database.CurrentTransaction
            ?? throw new InvalidOperationException("Purchase order transaction was not created.");

        try
        {
            var currentMaximum = await context.PurchaseOrders
                .Select(item => (int?)item.SequenceNumber)
                .MaxAsync(cancellationToken)
                ?? 0;
            var nextSequenceNumber = checked(currentMaximum + 1);
            var purchaseOrder = purchaseOrderFactory(nextSequenceNumber)
                ?? throw new InvalidOperationException("Purchase order factory returned null.");

            if (purchaseOrder.SequenceNumber != nextSequenceNumber)
            {
                throw new InvalidOperationException(
                    "Purchase order factory must preserve the reserved sequence number.");
            }

            context.PurchaseOrders.Add(purchaseOrder);
            await context.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);
            return purchaseOrder;
        }
        catch
        {
            await transaction.RollbackAsync(CancellationToken.None);
            throw;
        }
    }

    public async Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await FullQuery(context)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<PurchaseOrder?> GetByAwardAndSupplierAsync(
        Guid requestForQuotationAwardId,
        Guid supplierId,
        CancellationToken cancellationToken = default)
    {
        if (requestForQuotationAwardId == Guid.Empty || supplierId == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await FullQuery(context)
            .SingleOrDefaultAsync(
                item => item.RequestForQuotationAwardId == requestForQuotationAwardId
                    && item.SupplierId == supplierId,
                cancellationToken);
    }

    public async Task<IReadOnlyList<PurchaseOrder>> ListAsync(
        PurchaseOrderStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var query = FullQuery(context);
        if (status is not null)
        {
            query = query.Where(item => item.Status == status);
        }

        return await query
            .OrderByDescending(item => item.SequenceNumber)
            .ToArrayAsync(cancellationToken);
    }

    public async Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(purchaseOrder);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var persisted = await context.PurchaseOrders
            .Include(item => item.Lines)
            .SingleOrDefaultAsync(item => item.Id == purchaseOrder.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase order '{purchaseOrder.Id}' was not found.");

        var persistedLineIds = persisted.Lines.Select(line => line.Id).Order().ToArray();
        var incomingLineIds = purchaseOrder.Lines.Select(line => line.Id).Order().ToArray();
        if (!persistedLineIds.SequenceEqual(incomingLineIds))
        {
            throw new InvalidOperationException("Purchase order lines are immutable after creation.");
        }

        context.Entry(persisted).CurrentValues.SetValues(purchaseOrder);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<PurchaseOrder> FullQuery(ErpDbContext context) =>
        context.PurchaseOrders
            .AsNoTracking()
            .Include(item => item.Lines);

    private async Task<ErpDbContext> CreateImmediateTransactionalContextAsync(
        CancellationToken cancellationToken)
    {
        var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        await context.Database.OpenConnectionAsync(cancellationToken);

        if (context.Database.GetDbConnection() is not SqliteConnection connection)
        {
            await context.DisposeAsync();
            throw new InvalidOperationException(
                "Purchase order sequence allocation requires a Microsoft.Data.Sqlite connection.");
        }

        var transaction = connection.BeginTransaction(
            IsolationLevel.Serializable,
            deferred: false);
        context.Database.UseTransaction(transaction);
        return context;
    }
}
