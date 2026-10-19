using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Units;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class EfMeasurementUnitRepository(IDbContextFactory<ErpDbContext> contextFactory) : IMeasurementUnitRepository
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task AddAsync(MeasurementUnit unit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unit);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.MeasurementUnits.Add(unit);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<MeasurementUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.MeasurementUnits
            .AsNoTracking()
            .SingleOrDefaultAsync(unit => unit.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<MeasurementUnit>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<MeasurementUnit> query = context.MeasurementUnits.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(unit => unit.IsActive);
        }

        return await query
            .OrderBy(unit => unit.Name)
            .ThenBy(unit => unit.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(MeasurementUnit unit, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(unit);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.MeasurementUnits.Update(unit);
        await context.SaveChangesAsync(cancellationToken);
    }
}
