using LocalErp.Domain.Companies;

namespace LocalErp.Application.Abstractions.Persistence;

public interface ICompanyBranchRepository
{
    Task AddAsync(CompanyBranch branch, CancellationToken cancellationToken = default);

    Task<CompanyBranch?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CompanyBranch>> ListByCompanyAsync(
        Guid companyId,
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(CompanyBranch branch, CancellationToken cancellationToken = default);
}
