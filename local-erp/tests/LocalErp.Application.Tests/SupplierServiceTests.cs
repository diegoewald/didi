using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Application.Suppliers;
using LocalErp.Domain.Security;
using LocalErp.Domain.Suppliers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class SupplierServiceTests
{
    [TestMethod]
    public async Task CreateAsyncPersistsNormalizedSupplier()
    {
        var repository = new InMemorySupplierRepository();
        var service = new SupplierService(repository, new AllowAllAuthorizationService());

        var result = await service.CreateAsync("  Fornecedor Exemplo  ");

        Assert.AreEqual("Fornecedor Exemplo", result.Name);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual(1, repository.Suppliers.Count);
    }

    [TestMethod]
    public async Task ListAsyncDemandsSuppliersRead()
    {
        var authorization = new RecordingAuthorizationService();
        var service = new SupplierService(new InMemorySupplierRepository(), authorization);

        await service.ListAsync(includeInactive: true);

        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.SuppliersRead },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task MutationsDemandSuppliersManage()
    {
        var repository = new InMemorySupplierRepository();
        var supplier = new Supplier("Fornecedor Existente");
        await repository.AddAsync(supplier);
        var authorization = new RecordingAuthorizationService();
        var service = new SupplierService(repository, authorization);

        await service.CreateAsync("Novo Fornecedor");
        await service.RenameAsync(supplier.Id, "Fornecedor Renomeado");
        await service.SetActiveAsync(supplier.Id, isActive: false);

        CollectionAssert.AreEqual(
            new[]
            {
                PermissionCatalog.SuppliersManage,
                PermissionCatalog.SuppliersManage,
                PermissionCatalog.SuppliersManage,
            },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task RenameAndStatusChangesPersistDomainState()
    {
        var repository = new InMemorySupplierRepository();
        var supplier = new Supplier("Nome Antigo");
        await repository.AddAsync(supplier);
        var service = new SupplierService(repository, new AllowAllAuthorizationService());

        var renamed = await service.RenameAsync(supplier.Id, "  Nome Novo  ");
        var deactivated = await service.SetActiveAsync(supplier.Id, isActive: false);

        Assert.AreEqual("Nome Novo", renamed.Name);
        Assert.AreEqual("Nome Novo", repository.Suppliers.Single().Name);
        Assert.IsFalse(deactivated.IsActive);
    }

    [TestMethod]
    public async Task RenameAsyncRejectsUnknownSupplier()
    {
        var service = new SupplierService(
            new InMemorySupplierRepository(),
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

    private sealed class InMemorySupplierRepository : ISupplierRepository
    {
        public List<Supplier> Suppliers { get; } = [];

        public Task AddAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Suppliers.Add(supplier);
            return Task.CompletedTask;
        }

        public Task<Supplier?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Suppliers.SingleOrDefault(supplier => supplier.Id == id));
        }

        public Task<IReadOnlyList<Supplier>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<Supplier> result = Suppliers
                .Where(supplier => includeInactive || supplier.IsActive)
                .OrderBy(supplier => supplier.Name)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(Supplier supplier, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
