using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.CostCenters;
using LocalErp.Application.Security;
using LocalErp.Domain.CostCenters;
using LocalErp.Domain.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class CostCenterServiceTests
{
    [TestMethod]
    public async Task CreateAsyncPersistsNormalizedCostCenter()
    {
        var repository = new InMemoryCostCenterRepository();
        var service = new CostCenterService(repository, new AllowAllAuthorizationService());

        var result = await service.CreateAsync("  Produção  ");

        Assert.AreEqual("Produção", result.Name);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual(1, repository.CostCenters.Count);
    }

    [TestMethod]
    public async Task ListAsyncDemandsCostCentersRead()
    {
        var authorization = new RecordingAuthorizationService();
        var service = new CostCenterService(new InMemoryCostCenterRepository(), authorization);

        await service.ListAsync(includeInactive: true);

        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.CostCentersRead },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task MutationsDemandCostCentersManage()
    {
        var repository = new InMemoryCostCenterRepository();
        var costCenter = new CostCenter("Produção");
        await repository.AddAsync(costCenter);
        var authorization = new RecordingAuthorizationService();
        var service = new CostCenterService(repository, authorization);

        await service.CreateAsync("Administrativo");
        await service.RenameAsync(costCenter.Id, "Produção Industrial");
        await service.SetActiveAsync(costCenter.Id, isActive: false);

        CollectionAssert.AreEqual(
            new[]
            {
                PermissionCatalog.CostCentersManage,
                PermissionCatalog.CostCentersManage,
                PermissionCatalog.CostCentersManage,
            },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task RenameAndStatusChangesPersistDomainState()
    {
        var repository = new InMemoryCostCenterRepository();
        var costCenter = new CostCenter("Nome Antigo");
        await repository.AddAsync(costCenter);
        var service = new CostCenterService(repository, new AllowAllAuthorizationService());

        var renamed = await service.RenameAsync(costCenter.Id, "  Nome Novo  ");
        var deactivated = await service.SetActiveAsync(costCenter.Id, isActive: false);

        Assert.AreEqual("Nome Novo", renamed.Name);
        Assert.AreEqual("Nome Novo", repository.CostCenters.Single().Name);
        Assert.IsFalse(deactivated.IsActive);
    }

    [TestMethod]
    public async Task RenameAsyncRejectsUnknownCostCenter()
    {
        var service = new CostCenterService(
            new InMemoryCostCenterRepository(),
            new AllowAllAuthorizationService());

        await Assert.ThrowsExactlyAsync<KeyNotFoundException>(
            () => service.RenameAsync(Guid.NewGuid(), "Novo Nome"));
    }

    private sealed class RecordingAuthorizationService : IAuthorizationService
    {
        public List<string> Demands { get; } = [];

        public Task<bool> IsAllowedAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(true);
        }

        public Task DemandAsync(
            string permissionCode,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Demands.Add(permissionCode);
            return Task.CompletedTask;
        }
    }

    private sealed class InMemoryCostCenterRepository : ICostCenterRepository
    {
        public List<CostCenter> CostCenters { get; } = [];

        public Task AddAsync(CostCenter costCenter, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            CostCenters.Add(costCenter);
            return Task.CompletedTask;
        }

        public Task<CostCenter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(CostCenters.SingleOrDefault(costCenter => costCenter.Id == id));
        }

        public Task<IReadOnlyList<CostCenter>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<CostCenter> result = CostCenters
                .Where(costCenter => includeInactive || costCenter.IsActive)
                .OrderBy(costCenter => costCenter.Name)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(CostCenter costCenter, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
