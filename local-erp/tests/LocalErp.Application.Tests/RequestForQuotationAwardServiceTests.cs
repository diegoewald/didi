using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Abstractions.Time;
using LocalErp.Application.Purchasing;
using LocalErp.Application.Security;
using LocalErp.Application.Users;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class RequestForQuotationAwardServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 22, 10, 15, 0, DateTimeKind.Utc);

    [TestMethod]
    public async Task CreateDraftRequiresClosedRfqAndAwardPermission()
    {
        var rfqs = new InMemoryRequestForQuotationRepository();
        var openRfq = new RequestForQuotation(Guid.NewGuid(), Guid.NewGuid(), Now);
        await rfqs.AddAsync(openRfq);
        var authorization = new RecordingAuthorizationService();
        var service = NewService(requestForQuotations: rfqs, authorization: authorization);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.CreateDraftAsync(openRfq.Id));

        var fixture = ClosedRfq();
        await rfqs.AddAsync(fixture.RequestForQuotation);
        var created = await service.CreateDraftAsync(fixture.RequestForQuotation.Id, "  escolha  ");

        Assert.AreEqual(RequestForQuotationAwardStatus.Draft, created.Status);
        Assert.AreEqual("escolha", created.Notes);
        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.RequestForQuotationsAward, PermissionCatalog.RequestForQuotationsAward },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task OnlyOneAwardDecisionCanExistPerRfq()
    {
        var fixture = ClosedRfq();
        var rfqs = new InMemoryRequestForQuotationRepository();
        await rfqs.AddAsync(fixture.RequestForQuotation);
        var service = NewService(requestForQuotations: rfqs);

        await service.CreateDraftAsync(fixture.RequestForQuotation.Id);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.CreateDraftAsync(fixture.RequestForQuotation.Id));
    }

    [TestMethod]
    public async Task ReadsUseRfqReadPermission()
    {
        var fixture = ClosedRfq();
        var rfqs = new InMemoryRequestForQuotationRepository();
        await rfqs.AddAsync(fixture.RequestForQuotation);
        var authorization = new RecordingAuthorizationService();
        var service = NewService(requestForQuotations: rfqs, authorization: authorization);
        var created = await service.CreateDraftAsync(fixture.RequestForQuotation.Id);
        authorization.Demands.Clear();

        var byId = await service.GetByIdAsync(created.Id);
        var byRfq = await service.GetByRequestForQuotationIdAsync(fixture.RequestForQuotation.Id);
        var missing = await service.GetByRequestForQuotationIdAsync(Guid.NewGuid());

        Assert.AreEqual(created.Id, byId.Id);
        Assert.IsNotNull(byRfq);
        Assert.IsNull(missing);
        CollectionAssert.AreEqual(
            new[]
            {
                PermissionCatalog.RequestForQuotationsRead,
                PermissionCatalog.RequestForQuotationsRead,
                PermissionCatalog.RequestForQuotationsRead,
            },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task SelectionRequiresFinalizedQuotationLineFromSameRfqLine()
    {
        var fixture = ClosedRfq();
        var rfqs = new InMemoryRequestForQuotationRepository();
        await rfqs.AddAsync(fixture.RequestForQuotation);
        var service = NewService(requestForQuotations: rfqs);
        var award = await service.CreateDraftAsync(fixture.RequestForQuotation.Id);

        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.SelectLineAsync(
            award.Id,
            fixture.FirstRfqLineId,
            fixture.DraftQuotationLineId));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => service.SelectLineAsync(
            award.Id,
            fixture.SecondRfqLineId,
            fixture.FinalizedQuotationLineId));

        var selected = await service.SelectLineAsync(
            award.Id,
            fixture.FirstRfqLineId,
            fixture.FinalizedQuotationLineId);

        Assert.AreEqual(1, selected.Lines.Count);
        Assert.AreEqual(fixture.FirstRfqLineId, selected.Lines[0].RequestForQuotationLineId);
        Assert.AreEqual(fixture.FinalizedQuotationLineId, selected.Lines[0].SupplierQuotationLineId);
    }

    [TestMethod]
    public async Task FinalizeUsesAuthenticatedActiveActorAndMakesDecisionImmutable()
    {
        var fixture = ClosedRfq();
        var rfqs = new InMemoryRequestForQuotationRepository();
        await rfqs.AddAsync(fixture.RequestForQuotation);
        var actor = new UserResult(Guid.NewGuid(), "buyer", true);
        var authorization = new RecordingAuthorizationService();
        var service = NewService(
            requestForQuotations: rfqs,
            authorization: authorization,
            session: new TestSession(actor));
        var award = await service.CreateDraftAsync(fixture.RequestForQuotation.Id);
        await service.SelectLineAsync(
            award.Id,
            fixture.FirstRfqLineId,
            fixture.FinalizedQuotationLineId);

        var finalized = await service.FinalizeAsync(award.Id);

        Assert.AreEqual(RequestForQuotationAwardStatus.Finalized, finalized.Status);
        Assert.AreEqual(actor.Id, finalized.FinalizedByUserId);
        Assert.AreEqual(Now, finalized.FinalizedAtUtc);
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            service.UpdateDraftAsync(award.Id, "alterar"));
        Assert.IsTrue(authorization.Demands.All(code =>
            code == PermissionCatalog.RequestForQuotationsAward));
    }

    [TestMethod]
    public async Task MutationRejectsMissingOrInactiveSession()
    {
        var fixture = ClosedRfq();
        var rfqs = new InMemoryRequestForQuotationRepository();
        await rfqs.AddAsync(fixture.RequestForQuotation);

        var withoutSession = NewService(
            requestForQuotations: rfqs,
            session: new TestSession(null));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            withoutSession.CreateDraftAsync(fixture.RequestForQuotation.Id));

        var inactive = new UserResult(Guid.NewGuid(), "inactive", false);
        var inactiveSession = NewService(
            requestForQuotations: rfqs,
            session: new TestSession(inactive));
        await Assert.ThrowsExactlyAsync<InvalidOperationException>(() =>
            inactiveSession.CreateDraftAsync(fixture.RequestForQuotation.Id));
    }

    private static RequestForQuotationAwardService NewService(
        InMemoryRequestForQuotationAwardRepository? awards = null,
        InMemoryRequestForQuotationRepository? requestForQuotations = null,
        RecordingAuthorizationService? authorization = null,
        TestSession? session = null,
        TestClock? clock = null)
    {
        return new RequestForQuotationAwardService(
            awards ?? new InMemoryRequestForQuotationAwardRepository(),
            requestForQuotations ?? new InMemoryRequestForQuotationRepository(),
            authorization ?? new RecordingAuthorizationService(),
            session ?? new TestSession(new UserResult(Guid.NewGuid(), "buyer", true)),
            clock ?? new TestClock(Now));
    }

    private static ClosedRfqFixture ClosedRfq()
    {
        var rfq = new RequestForQuotation(Guid.NewGuid(), Guid.NewGuid(), Now);
        var firstLine = rfq.AddSourceLine(Guid.NewGuid());
        var secondLine = rfq.AddSourceLine(Guid.NewGuid());
        var finalizedSupplierId = Guid.NewGuid();
        var draftSupplierId = Guid.NewGuid();
        rfq.AddSupplier(finalizedSupplierId);
        rfq.AddSupplier(draftSupplierId);
        rfq.Send(Guid.NewGuid(), Now);

        var finalizedQuotation = rfq.CreateSupplierQuotation(
            finalizedSupplierId,
            "BRL",
            Guid.NewGuid(),
            Now);
        var finalizedQuotationLine = rfq.UpsertSupplierQuotationLine(
            finalizedQuotation.Id,
            firstLine.Id,
            10m);
        rfq.FinalizeSupplierQuotation(finalizedQuotation.Id, Guid.NewGuid(), Now);

        var draftQuotation = rfq.CreateSupplierQuotation(
            draftSupplierId,
            "BRL",
            Guid.NewGuid(),
            Now);
        var draftQuotationLine = rfq.UpsertSupplierQuotationLine(
            draftQuotation.Id,
            firstLine.Id,
            9m);

        rfq.Close(Guid.NewGuid(), Now);
        return new ClosedRfqFixture(
            rfq,
            firstLine.Id,
            secondLine.Id,
            finalizedQuotationLine.Id,
            draftQuotationLine.Id);
    }

    private sealed record ClosedRfqFixture(
        RequestForQuotation RequestForQuotation,
        Guid FirstRfqLineId,
        Guid SecondRfqLineId,
        Guid FinalizedQuotationLineId,
        Guid DraftQuotationLineId);

    private sealed class RecordingAuthorizationService : IAuthorizationService
    {
        public List<string> Demands { get; } = [];

        public Task<bool> IsAllowedAsync(string permissionCode, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }

        public Task DemandAsync(string permissionCode, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Demands.Add(permissionCode);
            return Task.CompletedTask;
        }
    }

    private sealed class TestSession(UserResult? currentUser) : IUserSession
    {
        public bool IsAuthenticated => CurrentUser is not null;

        public UserResult? CurrentUser { get; private set; } = currentUser;

        public void SignIn(UserResult user) => CurrentUser = user;

        public void SignOut() => CurrentUser = null;
    }

    private sealed class TestClock(DateTime utcNow) : IClock
    {
        public DateTime UtcNow { get; } = utcNow;
    }

    private sealed class InMemoryRequestForQuotationAwardRepository : IRequestForQuotationAwardRepository
    {
        public List<RequestForQuotationAward> Items { get; } = [];

        public Task AddAsync(RequestForQuotationAward award, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Items.Add(award);
            return Task.CompletedTask;
        }

        public Task<RequestForQuotationAward?> GetByIdAsync(
            Guid id,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        }

        public Task<RequestForQuotationAward?> GetByRequestForQuotationIdAsync(
            Guid requestForQuotationId,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(
                item => item.RequestForQuotationId == requestForQuotationId));
        }

        public Task UpdateAsync(RequestForQuotationAward award, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryRequestForQuotationRepository : IRequestForQuotationRepository
    {
        public List<RequestForQuotation> Items { get; } = [];

        public Task AddAsync(RequestForQuotation requestForQuotation, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Items.Add(requestForQuotation);
            return Task.CompletedTask;
        }

        public Task<RequestForQuotation?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Items.SingleOrDefault(item => item.Id == id));
        }

        public Task<IReadOnlyList<RequestForQuotation>> ListAsync(
            RequestForQuotationStatus? status = null,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<RequestForQuotation> result = Items
                .Where(item => status is null || item.Status == status)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(RequestForQuotation requestForQuotation, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
