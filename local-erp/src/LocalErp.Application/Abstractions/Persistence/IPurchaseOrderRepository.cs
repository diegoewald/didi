using LocalErp.Domain.Purchasing;

namespace LocalErp.Application.Abstractions.Persistence;

public interface IPurchaseOrderRepository
{
    Task<PurchaseOrder> AddWithNextSequenceAsync(
        Func<int, PurchaseOrder> purchaseOrderFactory,
        CancellationToken cancellationToken = default);

    Task<PurchaseOrder?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<PurchaseOrder?> GetByAwardAndSupplierAsync(
        Guid requestForQuotationAwardId,
        Guid supplierId,
        CancellationToken cancellationToken = default);

    Task<IReadOnlyList<PurchaseOrder>> ListAsync(
        PurchaseOrderStatus? status = null,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(PurchaseOrder purchaseOrder, CancellationToken cancellationToken = default);
}
