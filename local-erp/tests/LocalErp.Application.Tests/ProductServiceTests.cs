using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Products;
using LocalErp.Application.Security;
using LocalErp.Domain.Products;
using LocalErp.Domain.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class ProductServiceTests
{
    [TestMethod]
    public async Task CreateAsyncPersistsNormalizedProduct()
    {
        var repository = new InMemoryProductRepository();
        var service = new ProductService(repository, new AllowAllAuthorizationService());

        var result = await service.CreateAsync("  Produto Exemplo  ");

        Assert.AreEqual("Produto Exemplo", result.Name);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual(1, repository.Products.Count);
    }

    [TestMethod]
    public async Task ListAsyncDemandsProductsRead()
    {
        var authorization = new RecordingAuthorizationService();
        var service = new ProductService(new InMemoryProductRepository(), authorization);

        await service.ListAsync(includeInactive: true);

        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.ProductsRead },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task MutationsDemandProductsManage()
    {
        var repository = new InMemoryProductRepository();
        var product = new Product("Produto Existente");
        await repository.AddAsync(product);
        var authorization = new RecordingAuthorizationService();
        var service = new ProductService(repository, authorization);

        await service.CreateAsync("Novo Produto");
        await service.RenameAsync(product.Id, "Produto Renomeado");
        await service.SetActiveAsync(product.Id, isActive: false);

        CollectionAssert.AreEqual(
            new[]
            {
                PermissionCatalog.ProductsManage,
                PermissionCatalog.ProductsManage,
                PermissionCatalog.ProductsManage,
            },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task RenameAndStatusChangesPersistDomainState()
    {
        var repository = new InMemoryProductRepository();
        var product = new Product("Nome Antigo");
        await repository.AddAsync(product);
        var service = new ProductService(repository, new AllowAllAuthorizationService());

        var renamed = await service.RenameAsync(product.Id, "  Nome Novo  ");
        var deactivated = await service.SetActiveAsync(product.Id, isActive: false);

        Assert.AreEqual("Nome Novo", renamed.Name);
        Assert.AreEqual("Nome Novo", repository.Products.Single().Name);
        Assert.IsFalse(deactivated.IsActive);
    }

    [TestMethod]
    public async Task RenameAsyncRejectsUnknownProduct()
    {
        var service = new ProductService(
            new InMemoryProductRepository(),
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

    private sealed class InMemoryProductRepository : IProductRepository
    {
        public List<Product> Products { get; } = [];

        public Task AddAsync(Product product, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Products.Add(product);
            return Task.CompletedTask;
        }

        public Task<Product?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Products.SingleOrDefault(product => product.Id == id));
        }

        public Task<IReadOnlyList<Product>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<Product> result = Products
                .Where(product => includeInactive || product.IsActive)
                .OrderBy(product => product.Name)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(Product product, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
