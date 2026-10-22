using LocalErp.Domain.Purchasing;

namespace LocalErp.Application.Purchasing;

public sealed record RequestForQuotationAwardLineResult(
    Guid Id,
    Guid RequestForQuotationLineId,
    Guid SupplierQuotationLineId);

public sealed record RequestForQuotationAwardResult(
    Guid Id,
    Guid RequestForQuotationId,
    RequestForQuotationAwardStatus Status,
    Guid CreatedByUserId,
    DateTime CreatedAtUtc,
    string? Notes,
    Guid? FinalizedByUserId,
    DateTime? FinalizedAtUtc,
    IReadOnlyList<RequestForQuotationAwardLineResult> Lines)
{
    internal static RequestForQuotationAwardResult FromDomain(RequestForQuotationAward award)
    {
        ArgumentNullException.ThrowIfNull(award);
        return new RequestForQuotationAwardResult(
            award.Id,
            award.RequestForQuotationId,
            award.Status,
            award.CreatedByUserId,
            award.CreatedAtUtc,
            award.Notes,
            award.FinalizedByUserId,
            award.FinalizedAtUtc,
            award.Lines
                .OrderBy(line => line.RequestForQuotationLineId)
                .ThenBy(line => line.Id)
                .Select(line => new RequestForQuotationAwardLineResult(
                    line.Id,
                    line.RequestForQuotationLineId,
                    line.SupplierQuotationLineId))
                .ToArray());
    }
}
