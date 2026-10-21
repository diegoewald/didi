using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.CostCenters;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class EfCostCenterRepository(IDbContextFactory<ErpDbContext> contextFactory) : ICostCenterRepository
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task AddAsync(CostCenter costCenter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(costCenter);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.CostCenters.Add(costCenter);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CostCenter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.CostCenters
            .AsNoTracking()
            .SingleOrDefaultAsync(costCenter => costCenter.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CostCenter>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<CostCenter> query = context.CostCenters.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(costCenter => costCenter.IsActive);
        }

        return await query
            .OrderBy(costCenter => costCenter.Name)
            .ThenBy(costCenter => costCenter.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(CostCenter costCenter, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(costCenter);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.CostCenters.Update(costCenter);
        await context.SaveChangesAsync(cancellationToken);
    }
}
