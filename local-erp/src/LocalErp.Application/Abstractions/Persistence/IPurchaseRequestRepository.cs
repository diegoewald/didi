using LocalErp.Domain.Purchasing;

namespace LocalErp.Application.Abstractions.Persistence;

public interface IPurchaseRequestRepository
{
    Task AddAsync(PurchaseRequest purchaseRequest, CancellationToken cancellationToken = default);

    Task<PurchaseRequest?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseRequest>> ListAsync(
        PurchaseRequestStatus? status = null,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(PurchaseRequest purchaseRequest, CancellationToken cancellationToken = default);
}
