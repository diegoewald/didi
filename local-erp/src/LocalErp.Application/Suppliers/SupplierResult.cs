using LocalErp.Domain.Suppliers;

namespace LocalErp.Application.Suppliers;

public sealed record SupplierResult(Guid Id, string Name, bool IsActive)
{
    public static SupplierResult FromDomain(Supplier supplier)
    {
        ArgumentNullException.ThrowIfNull(supplier);
        return new SupplierResult(supplier.Id, supplier.Name, supplier.IsActive);
    }
}
