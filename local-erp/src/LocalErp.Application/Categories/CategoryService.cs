using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Domain.Categories;
using LocalErp.Domain.Security;

namespace LocalErp.Application.Categories;

public sealed class CategoryService(
    ICategoryRepository repository,
    IAuthorizationService authorizationService)
{
    private readonly ICategoryRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));

    public async Task<CategoryResult> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CategoriesManage, cancellationToken);
        var category = new Category(name);
        await _repository.AddAsync(category, cancellationToken);
        return CategoryResult.FromDomain(category);
    }

    public async Task<IReadOnlyList<CategoryResult>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CategoriesRead, cancellationToken);
        var categories = await _repository.ListAsync(includeInactive, cancellationToken);
        return categories.Select(CategoryResult.FromDomain).ToArray();
    }

    public async Task<CategoryResult> RenameAsync(
        Guid categoryId,
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CategoriesManage, cancellationToken);
        var category = await GetRequiredAsync(categoryId, cancellationToken);
        category.Rename(name);
        await _repository.UpdateAsync(category, cancellationToken);
        return CategoryResult.FromDomain(category);
    }

    public async Task<CategoryResult> SetActiveAsync(
        Guid categoryId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CategoriesManage, cancellationToken);
        var category = await GetRequiredAsync(categoryId, cancellationToken);

        if (isActive)
        {
            category.Activate();
        }
        else
        {
            category.Deactivate();
        }

        await _repository.UpdateAsync(category, cancellationToken);
        return CategoryResult.FromDomain(category);
    }

    private async Task<Category> GetRequiredAsync(
        Guid categoryId,
        CancellationToken cancellationToken)
    {
        if (categoryId == Guid.Empty)
        {
            throw new ArgumentException("Category identifier cannot be empty.", nameof(categoryId));
        }

        return await _repository.GetByIdAsync(categoryId, cancellationToken)
            ?? throw new KeyNotFoundException($"Category '{categoryId}' was not found.");
    }
}
