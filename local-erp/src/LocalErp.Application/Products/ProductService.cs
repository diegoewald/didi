using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Domain.Products;
using LocalErp.Domain.Security;

namespace LocalErp.Application.Products;

public sealed class ProductService(
    IProductRepository repository,
    IAuthorizationService authorizationService)
{
    private readonly IProductRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));

    public async Task<ProductResult> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.ProductsManage, cancellationToken);
        var product = new Product(name);
        await _repository.AddAsync(product, cancellationToken);
        return ProductResult.FromDomain(product);
    }

    public async Task<IReadOnlyList<ProductResult>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.ProductsRead, cancellationToken);
        var products = await _repository.ListAsync(includeInactive, cancellationToken);
        return products.Select(ProductResult.FromDomain).ToArray();
    }

    public async Task<ProductResult> RenameAsync(
        Guid productId,
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.ProductsManage, cancellationToken);
        var product = await GetRequiredAsync(productId, cancellationToken);
        product.Rename(name);
        await _repository.UpdateAsync(product, cancellationToken);
        return ProductResult.FromDomain(product);
    }

    public async Task<ProductResult> SetActiveAsync(
        Guid productId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.ProductsManage, cancellationToken);
        var product = await GetRequiredAsync(productId, cancellationToken);

        if (isActive)
        {
            product.Activate();
        }
        else
        {
            product.Deactivate();
        }

        await _repository.UpdateAsync(product, cancellationToken);
        return ProductResult.FromDomain(product);
    }

    private async Task<Product> GetRequiredAsync(Guid productId, CancellationToken cancellationToken)
    {
        if (productId == Guid.Empty)
        {
            throw new ArgumentException("Product identifier cannot be empty.", nameof(productId));
        }

        return await _repository.GetByIdAsync(productId, cancellationToken)
            ?? throw new KeyNotFoundException($"Product '{productId}' was not found.");
    }
}
