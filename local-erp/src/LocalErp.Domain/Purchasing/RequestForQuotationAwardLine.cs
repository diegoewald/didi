using LocalErp.Domain.Common;

namespace LocalErp.Domain.Purchasing;

public sealed class RequestForQuotationAwardLine : Entity
{
    private RequestForQuotationAwardLine()
    {
    }

    internal RequestForQuotationAwardLine(
        Guid requestForQuotationAwardId,
        Guid requestForQuotationLineId,
        Guid supplierQuotationLineId)
    {
        RequestForQuotationAwardId = ValidateIdentifier(
            requestForQuotationAwardId,
            nameof(requestForQuotationAwardId));
        RequestForQuotationLineId = ValidateIdentifier(
            requestForQuotationLineId,
            nameof(requestForQuotationLineId));
        SupplierQuotationLineId = ValidateIdentifier(
            supplierQuotationLineId,
            nameof(supplierQuotationLineId));
    }

    public Guid RequestForQuotationAwardId { get; private set; }

    public Guid RequestForQuotationLineId { get; private set; }

    public Guid SupplierQuotationLineId { get; private set; }

    internal void ReplaceSupplierQuotationLine(Guid supplierQuotationLineId)
    {
        SupplierQuotationLineId = ValidateIdentifier(
            supplierQuotationLineId,
            nameof(supplierQuotationLineId));
    }

    private static Guid ValidateIdentifier(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }

        return id;
    }
}
