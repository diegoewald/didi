using LocalErp.Domain.Common;

namespace LocalErp.Domain.Purchasing;

public sealed class RequestForQuotationLine : Entity
{
    private RequestForQuotationLine()
    {
    }

    internal RequestForQuotationLine(
        Guid requestForQuotationId,
        int lineNumber,
        Guid purchaseRequestLineId)
    {
        RequestForQuotationId = ValidateIdentifier(requestForQuotationId, nameof(requestForQuotationId));
        LineNumber = ValidateLineNumber(lineNumber);
        PurchaseRequestLineId = ValidateIdentifier(purchaseRequestLineId, nameof(purchaseRequestLineId));
    }

    public Guid RequestForQuotationId { get; private set; }

    public int LineNumber { get; private set; }

    public Guid PurchaseRequestLineId { get; private set; }

    private static Guid ValidateIdentifier(Guid id, string parameterName)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("Identifier cannot be empty.", parameterName);
        }

        return id;
    }

    private static int ValidateLineNumber(int lineNumber)
    {
        if (lineNumber <= 0)
        {
            throw new ArgumentOutOfRangeException(
                nameof(lineNumber),
                lineNumber,
                "RFQ line number must be greater than zero.");
        }

        return lineNumber;
    }
}
