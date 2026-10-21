using LocalErp.Domain.Purchasing;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class PurchaseRequestTests
{
    private static readonly DateTime CreatedAt = new(2026, 10, 21, 12, 0, 0, DateTimeKind.Utc);

    [TestMethod]
    public void ConstructorCreatesNormalizedDraft()
    {
        var requesterId = Guid.NewGuid();

        var request = new PurchaseRequest(requesterId, CreatedAt, "  Reposição preventiva  ");

        Assert.AreEqual(PurchaseRequestStatus.Draft, request.Status);
        Assert.AreEqual(requesterId, request.RequesterUserId);
        Assert.AreEqual(CreatedAt, request.CreatedAtUtc);
        Assert.AreEqual("Reposição preventiva", request.Notes);
        Assert.AreEqual(0, request.Lines.Count);
    }

    [TestMethod]
    public void ConstructorRejectsEmptyRequesterAndNonUtcTimestamp()
    {
        Assert.Throws<ArgumentException>(() => new PurchaseRequest(Guid.Empty, CreatedAt));
        Assert.Throws<ArgumentException>(() => new PurchaseRequest(
            Guid.NewGuid(),
            DateTime.SpecifyKind(CreatedAt, DateTimeKind.Local)));
    }

    [TestMethod]
    public void AddLineNormalizesDataAndAssignsStableSequence()
    {
        var request = NewRequest();
        var unitId = Guid.NewGuid();

        var first = request.AddLine("  Rolamento 6204  ", 2.5m, unitId, new DateOnly(2026, 11, 2));
        var second = request.AddLine("Parafuso", 10m, unitId);

        Assert.AreEqual(1, first.LineNumber);
        Assert.AreEqual("Rolamento 6204", first.Description);
        Assert.AreEqual(2.5m, first.Quantity);
        Assert.AreEqual(unitId, first.MeasurementUnitId);
        Assert.AreEqual(new DateOnly(2026, 11, 2), first.NeededByDate);
        Assert.AreEqual(2, second.LineNumber);
    }

    [TestMethod]
    public void AddLineRejectsInvalidDescriptionQuantityAndUnit()
    {
        var request = NewRequest();
        var unitId = Guid.NewGuid();

        Assert.Throws<ArgumentException>(() => request.AddLine("   ", 1m, unitId));
        Assert.Throws<ArgumentOutOfRangeException>(() => request.AddLine("Item", 0m, unitId));
        Assert.Throws<ArgumentException>(() => request.AddLine("Item", 1m, Guid.Empty));
    }

    [TestMethod]
    public void FailedLineUpdateDoesNotPartiallyMutateLine()
    {
        var request = NewRequest();
        var originalUnitId = Guid.NewGuid();
        var line = request.AddLine("Original", 2m, originalUnitId);

        Assert.Throws<ArgumentOutOfRangeException>(() => request.UpdateLine(
            line.Id,
            "Alterado",
            0m,
            Guid.NewGuid()));

        Assert.AreEqual("Original", line.Description);
        Assert.AreEqual(2m, line.Quantity);
        Assert.AreEqual(originalUnitId, line.MeasurementUnitId);
    }

    [TestMethod]
    public void SubmitRequiresAtLeastOneLineAndRecordsActor()
    {
        var request = NewRequest();
        var actorId = Guid.NewGuid();
        var submittedAt = CreatedAt.AddHours(1);

        Assert.Throws<InvalidOperationException>(() => request.Submit(actorId, submittedAt));

        request.AddLine("Item", 1m, Guid.NewGuid());
        request.Submit(actorId, submittedAt);

        Assert.AreEqual(PurchaseRequestStatus.Submitted, request.Status);
        Assert.AreEqual(actorId, request.SubmittedByUserId);
        Assert.AreEqual(submittedAt, request.SubmittedAtUtc);
    }

    [TestMethod]
    public void FailedSubmitDoesNotPartiallyMutateRequest()
    {
        var request = NewRequest();
        request.AddLine("Item", 1m, Guid.NewGuid());

        Assert.Throws<ArgumentException>(() => request.Submit(
            Guid.NewGuid(),
            DateTime.SpecifyKind(CreatedAt.AddHours(1), DateTimeKind.Local)));

        Assert.AreEqual(PurchaseRequestStatus.Draft, request.Status);
        Assert.IsNull(request.SubmittedByUserId);
        Assert.IsNull(request.SubmittedAtUtc);
    }

    [TestMethod]
    public void SubmittedRequestCannotBeEdited()
    {
        var request = NewRequest();
        var line = request.AddLine("Item", 1m, Guid.NewGuid());
        request.Submit(Guid.NewGuid(), CreatedAt.AddHours(1));

        Assert.Throws<InvalidOperationException>(() => request.UpdateNotes("Outra nota"));
        Assert.Throws<InvalidOperationException>(() => request.AddLine("Outro", 1m, Guid.NewGuid()));
        Assert.Throws<InvalidOperationException>(() => request.UpdateLine(line.Id, "Alterado", 2m, line.MeasurementUnitId));
        Assert.Throws<InvalidOperationException>(() => request.RemoveLine(line.Id));
    }

    [TestMethod]
    public void CancelRequiresReasonAndCreatesTerminalState()
    {
        var request = NewRequest();
        var actorId = Guid.NewGuid();
        var cancelledAt = CreatedAt.AddHours(2);

        Assert.Throws<ArgumentException>(() => request.Cancel(actorId, cancelledAt, "   "));
        Assert.AreEqual(PurchaseRequestStatus.Draft, request.Status);
        Assert.IsNull(request.CancelledByUserId);
        Assert.IsNull(request.CancelledAtUtc);

        request.Cancel(actorId, cancelledAt, "  Necessidade retirada  ");

        Assert.AreEqual(PurchaseRequestStatus.Cancelled, request.Status);
        Assert.AreEqual(actorId, request.CancelledByUserId);
        Assert.AreEqual(cancelledAt, request.CancelledAtUtc);
        Assert.AreEqual("Necessidade retirada", request.CancellationReason);
        Assert.Throws<InvalidOperationException>(() => request.Cancel(actorId, cancelledAt, "Outra"));
        Assert.Throws<InvalidOperationException>(() => request.UpdateNotes("Não permitido"));
    }

    [TestMethod]
    public void RemoveLineRequiresExistingLine()
    {
        var request = NewRequest();
        var line = request.AddLine("Item", 1m, Guid.NewGuid());

        request.RemoveLine(line.Id);

        Assert.AreEqual(0, request.Lines.Count);
        Assert.Throws<KeyNotFoundException>(() => request.RemoveLine(Guid.NewGuid()));
    }

    private static PurchaseRequest NewRequest() => new(Guid.NewGuid(), CreatedAt);
}
