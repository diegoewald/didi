using LocalErp.Domain.Purchasing;

namespace LocalErp.Application.Abstractions.Persistence;

public interface IRequestForQuotationAwardRepository
{
    Task AddAsync(RequestForQuotationAward award, CancellationToken cancellationToken = default);

    Task<RequestForQuotationAward?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default);

    Task<RequestForQuotationAward?> GetByRequestForQuotationIdAsync(
        Guid requestForQuotationId,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(RequestForQuotationAward award, CancellationToken cancellationToken = default);
}
