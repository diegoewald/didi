using LocalErp.Domain.Purchasing;

namespace LocalErp.Application.Abstractions.Persistence;

public interface IRequestForQuotationRepository
{
    Task AddAsync(RequestForQuotation requestForQuotation, CancellationToken cancellationToken = default);

    Task<RequestForQuotation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<RequestForQuotation>> ListAsync(
        RequestForQuotationStatus? status = null,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(RequestForQuotation requestForQuotation, CancellationToken cancellationToken = default);
}
