using LocalErp.Domain.Companies;

namespace LocalErp.Application.Abstractions.Persistence;

public interface ICompanyRepository
{
    Task AddAsync(Company company, CancellationToken cancellationToken = default);

    Task<Company?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Company>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Company company, CancellationToken cancellationToken = default);
}
