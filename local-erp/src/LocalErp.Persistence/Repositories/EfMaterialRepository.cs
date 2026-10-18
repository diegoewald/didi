using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Materials;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class EfMaterialRepository(IDbContextFactory<ErpDbContext> contextFactory) : IMaterialRepository
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task AddAsync(Material material, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(material);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Materials.Add(material);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Material?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Materials
            .AsNoTracking()
            .SingleOrDefaultAsync(material => material.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Material>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Material> query = context.Materials.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(material => material.IsActive);
        }

        return await query
            .OrderBy(material => material.Name)
            .ThenBy(material => material.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Material material, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(material);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Materials.Update(material);
        await context.SaveChangesAsync(cancellationToken);
    }
}
