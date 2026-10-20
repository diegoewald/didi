using LocalErp.Domain.Categories;

namespace LocalErp.Application.Categories;

public sealed record CategoryResult(Guid Id, string Name, bool IsActive)
{
    public static CategoryResult FromDomain(Category category)
    {
        ArgumentNullException.ThrowIfNull(category);
        return new CategoryResult(category.Id, category.Name, category.IsActive);
    }
}
