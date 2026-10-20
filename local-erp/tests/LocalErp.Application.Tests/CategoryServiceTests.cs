using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Categories;
using LocalErp.Application.Security;
using LocalErp.Domain.Categories;
using LocalErp.Domain.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Application.Tests;

[TestClass]
public sealed class CategoryServiceTests
{
    [TestMethod]
    public async Task CreateAsyncPersistsNormalizedCategory()
    {
        var repository = new InMemoryCategoryRepository();
        var service = new CategoryService(repository, new AllowAllAuthorizationService());

        var result = await service.CreateAsync("  Matéria-prima  ");

        Assert.AreEqual("Matéria-prima", result.Name);
        Assert.IsTrue(result.IsActive);
        Assert.AreEqual(1, repository.Categories.Count);
    }

    [TestMethod]
    public async Task ListAsyncDemandsCategoriesRead()
    {
        var authorization = new RecordingAuthorizationService();
        var service = new CategoryService(new InMemoryCategoryRepository(), authorization);

        await service.ListAsync(includeInactive: true);

        CollectionAssert.AreEqual(
            new[] { PermissionCatalog.CategoriesRead },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task MutationsDemandCategoriesManage()
    {
        var repository = new InMemoryCategoryRepository();
        var category = new Category("Matéria-prima");
        await repository.AddAsync(category);
        var authorization = new RecordingAuthorizationService();
        var service = new CategoryService(repository, authorization);

        await service.CreateAsync("Componente");
        await service.RenameAsync(category.Id, "Matéria-prima Renomeada");
        await service.SetActiveAsync(category.Id, isActive: false);

        CollectionAssert.AreEqual(
            new[]
            {
                PermissionCatalog.CategoriesManage,
                PermissionCatalog.CategoriesManage,
                PermissionCatalog.CategoriesManage,
            },
            authorization.Demands.ToArray());
    }

    [TestMethod]
    public async Task RenameAndStatusChangesPersistDomainState()
    {
        var repository = new InMemoryCategoryRepository();
        var category = new Category("Nome Antigo");
        await repository.AddAsync(category);
        var service = new CategoryService(repository, new AllowAllAuthorizationService());

        var renamed = await service.RenameAsync(category.Id, "  Nome Novo  ");
        var deactivated = await service.SetActiveAsync(category.Id, isActive: false);

        Assert.AreEqual("Nome Novo", renamed.Name);
        Assert.AreEqual("Nome Novo", repository.Categories.Single().Name);
        Assert.IsFalse(deactivated.IsActive);
    }

    [TestMethod]
    public async Task RenameAsyncRejectsUnknownCategory()
    {
        var service = new CategoryService(
            new InMemoryCategoryRepository(),
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

    private sealed class InMemoryCategoryRepository : ICategoryRepository
    {
        public List<Category> Categories { get; } = [];

        public Task AddAsync(Category category, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            Categories.Add(category);
            return Task.CompletedTask;
        }

        public Task<Category?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.FromResult(Categories.SingleOrDefault(category => category.Id == id));
        }

        public Task<IReadOnlyList<Category>> ListAsync(
            bool includeInactive,
            CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            IReadOnlyList<Category> result = Categories
                .Where(category => includeInactive || category.IsActive)
                .OrderBy(category => category.Name)
                .ToArray();
            return Task.FromResult(result);
        }

        public Task UpdateAsync(Category category, CancellationToken cancellationToken = default)
        {
            cancellationToken.ThrowIfCancellationRequested();
            return Task.CompletedTask;
        }
    }
}
