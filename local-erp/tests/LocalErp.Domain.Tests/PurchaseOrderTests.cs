using LocalErp.Domain.Purchasing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class PurchaseOrderTests
{
    private static readonly DateTime Now = new(2026, 10, 22, 14, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void NewPurchaseOrderCapturesSnapshotsAndCalculatesTotal()
    {
        var awardId = Guid.NewGuid();
        var supplierId = Guid.NewGuid();
        var actorId = Guid.NewGuid();
        var first = NewLine(quantity: 2m, unitPrice: 10.25m, description: "  Motor  ", unitName: "  UN  ");
        var second = NewLine(quantity: 3m, unitPrice: 4.5m);

        var order = new PurchaseOrder(
            7,
            awardId,
            supplierId,
            "  Fornecedor A  ",
            " brl ",
            actorId,
            Now,
            [first, second],
            "  urgente  ");

        Assert.AreEqual(7, order.SequenceNumber);
        Assert.AreEqual(awardId, order.RequestForQuotationAwardId);
        Assert.AreEqual(supplierId, order.SupplierId);
        Assert.AreEqual("Fornecedor A", order.SupplierNameSnapshot);
        Assert.AreEqual("BRL", order.CurrencyCode);
        Assert.AreEqual(PurchaseOrderStatus.Draft, order.Status);
        Assert.AreEqual(actorId, order.CreatedByUserId);
        Assert.AreEqual(Now, order.CreatedAtUtc);
        Assert.AreEqual("urgente", order.Notes);
        Assert.AreEqual(2, order.Lines.Count);
        Assert.AreEqual(1, order.Lines[0].LineNumber);
        Assert.AreEqual("Motor", order.Lines[0].DescriptionSnapshot);
        Assert.AreEqual("UN", order.Lines[0].MeasurementUnitNameSnapshot);
        Assert.AreEqual(20.50m, order.Lines[0].LineTotal);
        Assert.AreEqual(34.00m, order.Total);
    }

    [TestMethod]
    public void PurchaseOrderRequiresLinesAndUniqueAwardLineSnapshots()
    {
        Assert.ThrowsExactly<InvalidOperationException>(() => NewOrder([]));

        var line = NewLine();
        Assert.ThrowsExactly<InvalidOperationException>(() => NewOrder([line, line]));
    }

    [TestMethod]
    public void PurchaseOrderRejectsInvalidSnapshotValues()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => NewOrder([
            NewLine(quantity: 0m),
        ]));
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => NewOrder([
            NewLine(unitPrice: 1.1234567m),
        ]));
        Assert.ThrowsExactly<ArgumentException>(() => NewOrder([
            NewLine(description: "   "),
        ]));
    }

    [TestMethod]
    public void DraftCanUpdateNotesAndIssueOnce()
    {
        var order = NewOrder([NewLine()]);
        order.UpdateNotes("  revisão concluída  ");
        var actorId = Guid.NewGuid();

        order.Issue(actorId, Now);

        Assert.AreEqual(PurchaseOrderStatus.Issued, order.Status);
        Assert.AreEqual("revisão concluída", order.Notes);
        Assert.AreEqual(actorId, order.IssuedByUserId);
        Assert.AreEqual(Now, order.IssuedAtUtc);
        Assert.ThrowsExactly<InvalidOperationException>(() => order.UpdateNotes("outra"));
        Assert.ThrowsExactly<InvalidOperationException>(() => order.Issue(actorId, Now));
    }

    [TestMethod]
    public void DraftOrIssuedOrderCanBeCancelledWithReason()
    {
        var draft = NewOrder([NewLine()]);
        var actorId = Guid.NewGuid();
        draft.Cancel(actorId, Now, "  fornecedor indisponível  ");

        Assert.AreEqual(PurchaseOrderStatus.Cancelled, draft.Status);
        Assert.AreEqual(actorId, draft.CancelledByUserId);
        Assert.AreEqual(Now, draft.CancelledAtUtc);
        Assert.AreEqual("fornecedor indisponível", draft.CancellationReason);
        Assert.ThrowsExactly<InvalidOperationException>(() => draft.Issue(actorId, Now));

        var issued = NewOrder([NewLine()]);
        issued.Issue(actorId, Now);
        issued.Cancel(actorId, Now, "cancelado");
        Assert.AreEqual(PurchaseOrderStatus.Cancelled, issued.Status);
    }

    [TestMethod]
    public void FailedValidationDoesNotMutateExistingOrder()
    {
        var order = NewOrder([NewLine()]);
        order.UpdateNotes("original");
        var tooLongNotes = new string('n', PurchaseOrder.NotesMaxLength + 1);
        var tooLongReason = new string('r', PurchaseOrder.CancellationReasonMaxLength + 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => order.UpdateNotes(tooLongNotes));
        Assert.AreEqual("original", order.Notes);
        Assert.AreEqual(PurchaseOrderStatus.Draft, order.Status);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() =>
            order.Cancel(Guid.NewGuid(), Now, tooLongReason));
        Assert.AreEqual(PurchaseOrderStatus.Draft, order.Status);
        Assert.IsNull(order.CancellationReason);
    }

    private static PurchaseOrder NewOrder(IReadOnlyList<PurchaseOrderLineSnapshot> lines) =>
        new(
            1,
            Guid.NewGuid(),
            Guid.NewGuid(),
            "Fornecedor",
            "BRL",
            Guid.NewGuid(),
            Now,
            lines);

    private static PurchaseOrderLineSnapshot NewLine(
        decimal quantity = 1m,
        decimal unitPrice = 10m,
        string description = "Item",
        string unitName = "UN") =>
        new(
            Guid.NewGuid(),
            description,
            quantity,
            Guid.NewGuid(),
            unitName,
            unitPrice,
            new DateOnly(2026, 11, 1));
}
