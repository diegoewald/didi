using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Companies;
using Microsoft.EntityFrameworkCore;

namespace LocalErp.Persistence.Repositories;

public sealed class EfCompanyBranchRepository(IDbContextFactory<ErpDbContext> contextFactory)
    : ICompanyBranchRepository
{
    private readonly IDbContextFactory<ErpDbContext> _contextFactory = contextFactory
        ?? throw new ArgumentNullException(nameof(contextFactory));

    public async Task AddAsync(CompanyBranch branch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(branch);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.CompanyBranches.Add(branch);
        await context.SaveChangesAsync(cancellationToken);
    }

    public async Task<CompanyBranch?> GetByIdAsync(
        Guid id,
        CancellationToken cancellationToken = default)
    {
        if (id == Guid.Empty)
        {
            return null;
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.CompanyBranches
            .AsNoTracking()
            .SingleOrDefaultAsync(branch => branch.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<CompanyBranch>> ListByCompanyAsync(
        Guid companyId,
        bool includeInactive,
        CancellationToken cancellationToken = default)
    {
        if (companyId == Guid.Empty)
        {
            return [];
        }

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        IQueryable<CompanyBranch> query = context.CompanyBranches
            .AsNoTracking()
            .Where(branch => branch.CompanyId == companyId);

        if (!includeInactive)
        {
            query = query.Where(branch => branch.IsActive);
        }

        return await query
            .OrderBy(branch => branch.Name)
            .ThenBy(branch => branch.Id)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateAsync(CompanyBranch branch, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(branch);

        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        context.CompanyBranches.Update(branch);
        await context.SaveChangesAsync(cancellationToken);
    }
}
