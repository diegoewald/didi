using LocalErp.Domain.Units;

namespace LocalErp.Application.Abstractions.Persistence;

public interface IMeasurementUnitRepository
{
    Task AddAsync(MeasurementUnit unit, CancellationToken cancellationToken = default);

    Task<MeasurementUnit?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<MeasurementUnit>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(MeasurementUnit unit, CancellationToken cancellationToken = default);
}
