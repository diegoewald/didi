using LocalErp.Domain.CostCenters;

namespace LocalErp.Application.Abstractions.Persistence;

public interface ICostCenterRepository
{
    Task AddAsync(CostCenter costCenter, CancellationToken cancellationToken = default);

    Task<CostCenter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<CostCenter>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(CostCenter costCenter, CancellationToken cancellationToken = default);
}
