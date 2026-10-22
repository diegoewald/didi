using LocalErp.Domain.Purchasing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class RequestForQuotationAwardTests
{
    private static readonly DateTime Now = new(2026, 10, 22, 10, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void NewAwardStartsAsDraftAndNormalizesNotes()
    {
        var rfqId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var award = new RequestForQuotationAward(rfqId, userId, Now, "  escolha técnica  ");

        Assert.AreEqual(rfqId, award.RequestForQuotationId);
        Assert.AreEqual(userId, award.CreatedByUserId);
        Assert.AreEqual(Now, award.CreatedAtUtc);
        Assert.AreEqual(RequestForQuotationAwardStatus.Draft, award.Status);
        Assert.AreEqual("escolha técnica", award.Notes);
        Assert.AreEqual(0, award.Lines.Count);
    }

    [TestMethod]
    public void SelectLineUpsertsSelectionForSameRfqLine()
    {
        var award = NewAward();
        var rfqLineId = Guid.NewGuid();
        var firstQuotationLineId = Guid.NewGuid();
        var secondQuotationLineId = Guid.NewGuid();

        var first = award.SelectLine(rfqLineId, firstQuotationLineId);
        var updated = award.SelectLine(rfqLineId, secondQuotationLineId);

        Assert.AreSame(first, updated);
        Assert.AreEqual(1, award.Lines.Count);
        Assert.AreEqual(secondQuotationLineId, award.Lines.Single().SupplierQuotationLineId);
    }

    [TestMethod]
    public void SameSupplierQuotationLineCannotBeUsedForDifferentRfqLines()
    {
        var award = NewAward();
        var supplierQuotationLineId = Guid.NewGuid();
        award.SelectLine(Guid.NewGuid(), supplierQuotationLineId);

        Assert.ThrowsExactly<InvalidOperationException>(() =>
            award.SelectLine(Guid.NewGuid(), supplierQuotationLineId));
    }

    [TestMethod]
    public void FinalizeRequiresAtLeastOneSelection()
    {
        var award = NewAward();

        Assert.ThrowsExactly<InvalidOperationException>(() => award.Finalize(Guid.NewGuid(), Now));

        award.SelectLine(Guid.NewGuid(), Guid.NewGuid());
        var actor = Guid.NewGuid();
        award.Finalize(actor, Now);

        Assert.AreEqual(RequestForQuotationAwardStatus.Finalized, award.Status);
        Assert.AreEqual(actor, award.FinalizedByUserId);
        Assert.AreEqual(Now, award.FinalizedAtUtc);
    }

    [TestMethod]
    public void FinalizedAwardIsImmutable()
    {
        var award = NewAward();
        var rfqLineId = Guid.NewGuid();
        award.SelectLine(rfqLineId, Guid.NewGuid());
        award.Finalize(Guid.NewGuid(), Now);

        Assert.ThrowsExactly<InvalidOperationException>(() => award.UpdateNotes("nova"));
        Assert.ThrowsExactly<InvalidOperationException>(() => award.SelectLine(rfqLineId, Guid.NewGuid()));
        Assert.ThrowsExactly<InvalidOperationException>(() => award.RemoveLineSelection(rfqLineId));
    }

    [TestMethod]
    public void InvalidNotesDoNotPartiallyMutateDraft()
    {
        var award = new RequestForQuotationAward(Guid.NewGuid(), Guid.NewGuid(), Now, "original");
        var tooLong = new string('x', RequestForQuotationAward.NotesMaxLength + 1);

        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => award.UpdateNotes(tooLong));
        Assert.AreEqual("original", award.Notes);
    }

    private static RequestForQuotationAward NewAward() =>
        new(Guid.NewGuid(), Guid.NewGuid(), Now);
}
