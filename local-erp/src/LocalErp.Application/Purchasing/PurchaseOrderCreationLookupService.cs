using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Security;

namespace LocalErp.Application.Purchasing;

public sealed class PurchaseOrderCreationLookupService(
    IPurchaseOrderRepository purchaseOrderRepository,
    IRequestForQuotationAwardRepository awardRepository,
    IRequestForQuotationRepository requestForQuotationRepository,
    ISupplierRepository supplierRepository,
    IAuthorizationService authorizationService)
{
    private readonly IPurchaseOrderRepository _purchaseOrderRepository = purchaseOrderRepository
        ?? throw new ArgumentNullException(nameof(purchaseOrderRepository));
    private readonly IRequestForQuotationAwardRepository _awardRepository = awardRepository
        ?? throw new ArgumentNullException(nameof(awardRepository));
    private readonly IRequestForQuotationRepository _requestForQuotationRepository = requestForQuotationRepository
        ?? throw new ArgumentNullException(nameof(requestForQuotationRepository));
    private readonly ISupplierRepository _supplierRepository = supplierRepository
        ?? throw new ArgumentNullException(nameof(supplierRepository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));

    public async Task<IReadOnlyList<PurchaseOrderCreationCandidateResult>> ListAvailableAsync(
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.PurchaseOrdersManage, cancellationToken);

        var closedRequests = await _requestForQuotationRepository.ListAsync(
            RequestForQuotationStatus.Closed,
            cancellationToken);
        var result = new List<PurchaseOrderCreationCandidateResult>();

        foreach (var requestForQuotation in closedRequests)
        {
            var award = await _awardRepository.GetByRequestForQuotationIdAsync(
                requestForQuotation.Id,
                cancellationToken);
            if (award is null || award.Status != RequestForQuotationAwardStatus.Finalized)
            {
                continue;
            }

            var finalizedAtUtc = award.FinalizedAtUtc
                ?? throw new InvalidOperationException("A finalized RFQ award is missing its finalization timestamp.");
            var awardedLineCounts = ResolveAwardedLineCounts(award, requestForQuotation);

            foreach (var (supplierId, awardedLineCount) in awardedLineCounts)
            {
                if (await _purchaseOrderRepository.GetByAwardAndSupplierAsync(
                        award.Id,
                        supplierId,
                        cancellationToken) is not null)
                {
                    continue;
                }

                var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken)
                    ?? throw new InvalidOperationException(
                        $"A finalized RFQ award references supplier '{supplierId}' that no longer exists.");
                if (!supplier.IsActive)
                {
                    continue;
                }

                result.Add(new PurchaseOrderCreationCandidateResult(
                    award.Id,
                    requestForQuotation.Id,
                    finalizedAtUtc,
                    supplier.Id,
                    supplier.Name,
                    awardedLineCount));
            }
        }

        return result
            .OrderByDescending(candidate => candidate.AwardFinalizedAtUtc)
            .ThenBy(candidate => candidate.SupplierName, StringComparer.OrdinalIgnoreCase)
            .ThenBy(candidate => candidate.RequestForQuotationAwardId)
            .ThenBy(candidate => candidate.SupplierId)
            .ToArray();
    }

    private static Dictionary<Guid, int> ResolveAwardedLineCounts(
        RequestForQuotationAward award,
        RequestForQuotation requestForQuotation)
    {
        var result = new Dictionary<Guid, int>();
        foreach (var awardLine in award.Lines)
        {
            var requestForQuotationLine = requestForQuotation.Lines.SingleOrDefault(
                line => line.Id == awardLine.RequestForQuotationLineId)
                ?? throw new InvalidOperationException(
                    "An RFQ award references an RFQ line that does not exist.");
            var selected = FindSupplierQuotationLine(
                requestForQuotation,
                awardLine.SupplierQuotationLineId);

            if (selected.Quotation.Status != SupplierQuotationStatus.Finalized)
            {
                throw new InvalidOperationException(
                    "Purchase order creation candidates require finalized supplier quotation lines.");
            }

            if (selected.Line.RequestForQuotationLineId != requestForQuotationLine.Id)
            {
                throw new InvalidOperationException(
                    "An awarded supplier quotation line does not match its RFQ line.");
            }

            result[selected.Quotation.SupplierId] =
                result.GetValueOrDefault(selected.Quotation.SupplierId) + 1;
        }

        return result;
    }

    private static (SupplierQuotation Quotation, SupplierQuotationLine Line) FindSupplierQuotationLine(
        RequestForQuotation requestForQuotation,
        Guid supplierQuotationLineId)
    {
        foreach (var quotation in requestForQuotation.SupplierQuotations)
        {
            var line = quotation.Lines.SingleOrDefault(item => item.Id == supplierQuotationLineId);
            if (line is not null)
            {
                return (quotation, line);
            }
        }

        throw new InvalidOperationException(
            "An RFQ award references a supplier quotation line that does not exist.");
    }
}
