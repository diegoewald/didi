using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Materials;
using LocalErp.Application.Security;
using LocalErp.Domain.Materials;
using LocalErp.Domain.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class MaterialServiceTests
{
    [TestMethod]
    public async Task CreateAsyncPersistsNormalizedMaterial()
    {
        var repository = new InMemoryMaterialRepository();
        var service = new MaterialService(repository, new AllowAllAuthorizationService());

        var result = await service.CreateAsync("  Material Exemplo  ");

        Assert.AreEqual("Material Exemplo", result.Name);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual(1, repository.Materials.Count);
    }

    [TestMethod]
    public async Task ListAsyncDemandsMaterialsRead()
    {
        var authorization = new RecordingAuthorizationService();
        var service = new MaterialService(new InMemoryMaterialRepository(), authorization);

        await service.ListAsync(includeInactive: true);

        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.MaterialsRead },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task MutationsDemandMaterialsManage()
    {
        var repository = new InMemoryMaterialRepository();
        var material = new Material("Material Existente");
        await repository.AddAsync(material);
        var authorization = new RecordingAuthorizationService();
        var service = new MaterialService(repository, authorization);

        await service.CreateAsync("Novo Material");
        await service.RenameAsync(material.Id, "Material Renomeado");
        await service.SetActiveAsync(material.Id, isActive: false);

        CollectionAssert.AreEqual(
            new[]
            {
                PermissionCatalog.MaterialsManage,
                PermissionCatalog.MaterialsManage,
                PermissionCatalog.MaterialsManage,
            },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task RenameAndStatusChangesPersistDomainState()
    {
        var repository = new InMemoryMaterialRepository();
        var material = new Material("Nome Antigo");
        await repository.AddAsync(material);
        var service = new MaterialService(repository, new AllowAllAuthorizationService());

        var renamed = await service.RenameAsync(material.Id, "  Nome Novo  ");
        var deactivated = await service.SetActiveAsync(material.Id, isActive: false);

        Assert.AreEqual("Nome Novo", renamed.Name);
        Assert.AreEqual("Nome Novo", repository.Materials.Single().Name);
        Assert.IsFalse(deactivated.IsActive);
    }

    [TestMethod]
    public async Task RenameAsyncRejectsUnknownMaterial()
    {
        var service = new MaterialService(
            new InMemoryMaterialRepository(),
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

    private sealed class InMemoryMaterialRepository : IMaterialRepository
    {
        public List<Material> Materials { get; } = [];

        public Task AddAsync(Material material, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Materials.Add(material);
            return Task.CompletedTask;
        }

        public Task<Material?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Materials.SingleOrDefault(material => material.Id == id));
        }

        public Task<IReadOnlyList<Material>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<Material> result = Materials
                .Where(material => includeInactive || material.IsActive)
                .OrderBy(material => material.Name)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(Material material, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
