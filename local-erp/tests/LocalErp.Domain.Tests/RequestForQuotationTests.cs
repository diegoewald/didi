using LocalErp.Domain.Purchasing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class RequestForQuotationTests
{
    private static readonly DateTime Now = new(2026, 10, 21, 13, 30, 0, DateTimeKind.Utc);

    [TestMethod]
    public void NewRfqStartsAsDraftAndNormalizesNotes()
    {
        var rfq = new RequestForQuotation(Guid.NewGuid(), Guid.NewGuid(), Now, "  urgente  ");

        Assert.AreEqual(RequestForQuotationStatus.Draft, rfq.Status);
        Assert.AreEqual("urgente", rfq.Notes);
        Assert.AreEqual(0, rfq.Lines.Count);
        Assert.AreEqual(0, rfq.Suppliers.Count);
    }

    [TestMethod]
    public void DuplicateSourceLinesAndSuppliersAreRejected()
    {
        var rfq = NewRfq();
        var sourceLineId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        rfq.AddSourceLine(sourceLineId);
        rfq.AddSupplier(supplierId);

        Assert.ThrowsExactly<InvalidOperationException>(() => rfq.AddSourceLine(sourceLineId));
        Assert.ThrowsExactly<InvalidOperationException>(() => rfq.AddSupplier(supplierId));
    }

    [TestMethod]
    public void SendRequiresAtLeastOneLineAndOneSupplier()
    {
        var rfq = NewRfq();

        Assert.ThrowsExactly<InvalidOperationException>(() => rfq.Send(Guid.NewGuid(), Now));

        rfq.AddSourceLine(Guid.NewGuid());
        Assert.ThrowsExactly<InvalidOperationException>(() => rfq.Send(Guid.NewGuid(), Now));

        rfq.AddSupplier(Guid.NewGuid());
        rfq.Send(Guid.NewGuid(), Now);

        Assert.AreEqual(RequestForQuotationStatus.Sent, rfq.Status);
        Assert.ThrowsExactly<InvalidOperationException>(() => rfq.AddSourceLine(Guid.NewGuid()));
    }

    [TestMethod]
    public void SupplierQuotationRequiresInvitedSupplierAndNormalizesCurrency()
    {
        var rfq = SentRfq(out var invitedSupplierId, out _);

        Assert.ThrowsExactly<InvalidOperationException>(() => rfq.CreateSupplierQuotation(
            Guid.NewGuid(), "BRL", Guid.NewGuid(), Now));

        var quotation = rfq.CreateSupplierQuotation(
            invitedSupplierId,
            " brl ",
            Guid.NewGuid(),
            Now,
            "  Q-100  ");

        Assert.AreEqual("BRL", quotation.CurrencyCode);
        Assert.AreEqual("Q-100", quotation.SupplierReference);
        Assert.AreEqual(SupplierQuotationStatus.Draft, quotation.Status);
    }

    [TestMethod]
    public void SupplierQuotationLineRejectsNegativeAndMoreThanSixDecimals()
    {
        var rfq = SentRfq(out var supplierId, out var rfqLineId);
        var quotation = rfq.CreateSupplierQuotation(supplierId, "USD", Guid.NewGuid(), Now);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => rfq.UpsertSupplierQuotationLine(
            quotation.Id, rfqLineId, -1m));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => rfq.UpsertSupplierQuotationLine(
            quotation.Id, rfqLineId, 1.0000001m));

        var line = rfq.UpsertSupplierQuotationLine(quotation.Id, rfqLineId, 12.345678m);
        Assert.AreEqual(12.345678m, line.UnitPrice);
    }

    [TestMethod]
    public void FinalizedSupplierQuotationIsImmutableAndAllowsRfqClosing()
    {
        var rfq = SentRfq(out var supplierId, out var rfqLineId);
        var quotation = rfq.CreateSupplierQuotation(supplierId, "BRL", Guid.NewGuid(), Now);

        Assert.ThrowsExactly<InvalidOperationException>(() => rfq.Close(Guid.NewGuid(), Now));
        Assert.ThrowsExactly<InvalidOperationException>(() => rfq.FinalizeSupplierQuotation(
            quotation.Id, Guid.NewGuid(), Now));

        rfq.UpsertSupplierQuotationLine(quotation.Id, rfqLineId, 10m);
        rfq.FinalizeSupplierQuotation(quotation.Id, Guid.NewGuid(), Now);

        Assert.AreEqual(SupplierQuotationStatus.Finalized, quotation.Status);
        Assert.ThrowsExactly<InvalidOperationException>(() => rfq.UpdateSupplierQuotation(
            quotation.Id, "USD", null, null, null));

        rfq.Close(Guid.NewGuid(), Now);
        Assert.AreEqual(RequestForQuotationStatus.Closed, rfq.Status);
    }

    [TestMethod]
    public void CancelRequiresReasonAndIsTerminal()
    {
        var rfq = NewRfq();

        Assert.ThrowsExactly<ArgumentException>(() => rfq.Cancel(Guid.NewGuid(), Now, "   "));
        rfq.Cancel(Guid.NewGuid(), Now, "  demanda retirada  ");

        Assert.AreEqual(RequestForQuotationStatus.Cancelled, rfq.Status);
        Assert.AreEqual("demanda retirada", rfq.CancellationReason);
        Assert.ThrowsExactly<InvalidOperationException>(() => rfq.UpdateNotes("x"));
    }

    private static RequestForQuotation NewRfq() => new(Guid.NewGuid(), Guid.NewGuid(), Now);

    private static RequestForQuotation SentRfq(out Guid supplierId, out Guid rfqLineId)
    {
        var rfq = NewRfq();
        var line = rfq.AddSourceLine(Guid.NewGuid());
        supplierId = Guid.NewGuid();
        rfq.AddSupplier(supplierId);
        rfq.Send(Guid.NewGuid(), Now);
        rfqLineId = line.Id;
        return rfq;
    }
}
