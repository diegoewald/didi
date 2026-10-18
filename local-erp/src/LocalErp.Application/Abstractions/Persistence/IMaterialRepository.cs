using LocalErp.Domain.Materials;

namespace LocalErp.Application.Abstractions.Persistence;

public interface IMaterialRepository
{
    Task AddAsync(Material material, CancellationToken cancellationToken = default);

    Task<Material?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Material>> ListAsync(
        bool includeInactive,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(Material material, CancellationToken cancellationToken = default);
}
