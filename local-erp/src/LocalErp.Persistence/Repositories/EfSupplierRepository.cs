using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Suppliers;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class EfSupplierRepository(IDbContextFactory<ErpDbContext> contextFactory) : ISupplierRepository
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Suppliers.Add(supplier);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Suppliers
            .AsNoTracking()
            .SingleOrDefaultAsync(supplier => supplier.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Supplier>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Supplier> query = context.Suppliers.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(supplier => supplier.IsActive);
        }

        return await query
            .OrderBy(supplier => supplier.Name)
            .ThenBy(supplier => supplier.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplier);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Suppliers.Update(supplier);
        await context.SaveChangesAsync(cancellationToken);
    }
}
