using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class EfProductRepository(IDbContextFactory<ErpDbContext> contextFactory) : IProductRepository
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task AddAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Products.Add(product);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Products
            .AsNoTracking()
            .SingleOrDefaultAsync(product => product.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Product>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Product> query = context.Products.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(product => product.IsActive);
        }

        return await query
            .OrderBy(product => product.Name)
            .ThenBy(product => product.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(product);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Products.Update(product);
        await context.SaveChangesAsync(cancellationToken);
    }
}
