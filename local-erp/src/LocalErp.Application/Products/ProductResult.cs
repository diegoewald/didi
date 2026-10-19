using LocalErp.Domain.Products;

namespace LocalErp.Application.Products;

public sealed record ProductResult(Guid Id, string Name, bool IsActive)
{
    public static ProductResult FromDomain(Product product)
    {
        ArgumentNullException.ThrowIfNull(product);
        return new ProductResult(product.Id, product.Name, product.IsActive);
    }
}
