using LocalErp.Domain.Common;

namespace LocalErp.Domain.Purchasing;

public sealed class RequestForQuotationSupplier : Entity
{
    private RequestForQuotationSupplier()
    {
    }

    internal RequestForQuotationSupplier(Guid requestForQuotationId, Guid supplierId)
    {
        RequestForQuotationId = ValidateIdentifier(requestForQuotationId, nameof(requestForQuotationId));
        SupplierId = ValidateIdentifier(supplierId, nameof(supplierId));
    }

    public Guid RequestForQuotationId { get; private set; }

    public Guid SupplierId { get; private set; }

    private static Guid ValidateIdentifier(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }

        return id;
    }
}
