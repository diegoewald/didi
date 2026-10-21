using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Purchasing;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class EfRequestForQuotationRepository(IDbContextFactory<ErpDbContext> contextFactory)
    : IRequestForQuotationRepository
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task AddAsync(RequestForQuotation requestForQuotation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestForQuotation);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.RequestForQuotations.Add(requestForQuotation);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<RequestForQuotation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await FullQuery(context)
            .SingleOrDefaultAsync(item => item.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<RequestForQuotation>> ListAsync(
        RequestForQuotationStatus? status = null,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<RequestForQuotation> query = FullQuery(context);
        if (status is RequestForQuotationStatus requestedStatus)
        {
            query = query.Where(item => item.Status == requestedStatus);
        }

        return await query
            .OrderByDescending(item => item.CreatedAtUtc)
            .ThenBy(item => item.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(RequestForQuotation requestForQuotation, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(requestForQuotation);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var persisted = await context.RequestForQuotations
            .Include(item => item.Lines)
            .Include(item => item.Suppliers)
            .Include(item => item.SupplierQuotations)
                .ThenInclude(quotation => quotation.Lines)
            .SingleOrDefaultAsync(item => item.Id == requestForQuotation.Id, cancellationToken)
            ?? throw new KeyNotFoundException($"RFQ '{requestForQuotation.Id}' was not found.");

        context.Entry(persisted).CurrentValues.SetValues(requestForQuotation);
        SyncLines(context, persisted, requestForQuotation);
        SyncSuppliers(context, persisted, requestForQuotation);
        SyncSupplierQuotations(context, persisted, requestForQuotation);
        await context.SaveChangesAsync(cancellationToken);
    }

    private static IQueryable<RequestForQuotation> FullQuery(ErpDbContext context) => context.RequestForQuotations
        .AsNoTracking()
        .AsSplitQuery()
        .Include(item => item.Lines)
        .Include(item => item.Suppliers)
        .Include(item => item.SupplierQuotations)
            .ThenInclude(quotation => quotation.Lines);

    private static void SyncLines(
        ErpDbContext context,
        RequestForQuotation persisted,
        RequestForQuotation incoming)
    {
        var incomingById = incoming.Lines.ToDictionary(line => line.Id);
        foreach (var persistedLine in persisted.Lines.ToArray())
        {
            if (!incomingById.TryGetValue(persistedLine.Id, out var incomingLine))
            {
                context.RequestForQuotationLines.Remove(persistedLine);
                continue;
            }

            context.Entry(persistedLine).CurrentValues.SetValues(incomingLine);
            incomingById.Remove(persistedLine.Id);
        }

        foreach (var newLine in incomingById.Values)
        {
            context.RequestForQuotationLines.Add(newLine);
        }
    }

    private static void SyncSuppliers(
        ErpDbContext context,
        RequestForQuotation persisted,
        RequestForQuotation incoming)
    {
        var incomingById = incoming.Suppliers.ToDictionary(item => item.Id);
        foreach (var persistedSupplier in persisted.Suppliers.ToArray())
        {
            if (!incomingById.TryGetValue(persistedSupplier.Id, out var incomingSupplier))
            {
                context.RequestForQuotationSuppliers.Remove(persistedSupplier);
                continue;
            }

            context.Entry(persistedSupplier).CurrentValues.SetValues(incomingSupplier);
            incomingById.Remove(persistedSupplier.Id);
        }

        foreach (var newSupplier in incomingById.Values)
        {
            context.RequestForQuotationSuppliers.Add(newSupplier);
        }
    }

    private static void SyncSupplierQuotations(
        ErpDbContext context,
        RequestForQuotation persisted,
        RequestForQuotation incoming)
    {
        var incomingById = incoming.SupplierQuotations.ToDictionary(item => item.Id);
        foreach (var persistedQuotation in persisted.SupplierQuotations.ToArray())
        {
            if (!incomingById.TryGetValue(persistedQuotation.Id, out var incomingQuotation))
            {
                context.SupplierQuotations.Remove(persistedQuotation);
                continue;
            }

            context.Entry(persistedQuotation).CurrentValues.SetValues(incomingQuotation);
            SyncSupplierQuotationLines(context, persistedQuotation, incomingQuotation);
            incomingById.Remove(persistedQuotation.Id);
        }

        foreach (var newQuotation in incomingById.Values)
        {
            context.SupplierQuotations.Add(newQuotation);
        }
    }

    private static void SyncSupplierQuotationLines(
        ErpDbContext context,
        SupplierQuotation persisted,
        SupplierQuotation incoming)
    {
        var incomingById = incoming.Lines.ToDictionary(line => line.Id);
        foreach (var persistedLine in persisted.Lines.ToArray())
        {
            if (!incomingById.TryGetValue(persistedLine.Id, out var incomingLine))
            {
                context.SupplierQuotationLines.Remove(persistedLine);
                continue;
            }

            context.Entry(persistedLine).CurrentValues.SetValues(incomingLine);
            incomingById.Remove(persistedLine.Id);
        }

        foreach (var newLine in incomingById.Values)
        {
            context.SupplierQuotationLines.Add(newLine);
        }
    }
}
