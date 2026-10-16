using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Companies;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class EfCompanyRepository(IDbContextFactory<ErpDbContext> contextFactory) : ICompanyRepository
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task AddAsync(Company company, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(company);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Companies.Add(company);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.Companies
            .AsNoTracking()
            .SingleOrDefaultAsync(company => company.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Company>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        IQueryable<Company> query = context.Companies.AsNoTracking();
        if (!includeInactive)
        {
            query = query.Where(company => company.IsActive);
        }

        return await query
            .OrderBy(company => company.Name)
            .ThenBy(company => company.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(Company company, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(company);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.Companies.Update(company);
        await context.SaveChangesAsync(cancellationToken);
    }
}
