using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Domain.Security;
using LocalErp.Domain.Suppliers;

namespace LocalErp.Application.Suppliers;

public sealed class SupplierService(
    ISupplierRepository repository,
    IAuthorizationService authorizationService)
{
    private readonly ISupplierRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));

    public async Task<SupplierResult> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.SuppliersManage, cancellationToken);
        var supplier = new Supplier(name);
        await _repository.AddAsync(supplier, cancellationToken);
        return SupplierResult.FromDomain(supplier);
    }

    public async Task<IReadOnlyList<SupplierResult>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.SuppliersRead, cancellationToken);
        var suppliers = await _repository.ListAsync(includeInactive, cancellationToken);
        return suppliers.Select(SupplierResult.FromDomain).ToArray();
    }

    public async Task<SupplierResult> RenameAsync(
        Guid supplierId,
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.SuppliersManage, cancellationToken);
        var supplier = await GetRequiredAsync(supplierId, cancellationToken);
        supplier.Rename(name);
        await _repository.UpdateAsync(supplier, cancellationToken);
        return SupplierResult.FromDomain(supplier);
    }

    public async Task<SupplierResult> SetActiveAsync(
        Guid supplierId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.SuppliersManage, cancellationToken);
        var supplier = await GetRequiredAsync(supplierId, cancellationToken);

        if (isActive)
        {
            supplier.Activate();
        }
        else
        {
            supplier.Deactivate();
        }

        await _repository.UpdateAsync(supplier, cancellationToken);
        return SupplierResult.FromDomain(supplier);
    }

    private async Task<Supplier> GetRequiredAsync(Guid supplierId, CancellationToken cancellationToken)
    {
        if (supplierId == Guid.Empty)
        {
            throw new ArgumentException("Supplier identifier cannot be empty.", nameof(supplierId));
        }

        return await _repository.GetByIdAsync(supplierId, cancellationToken)
            ?? throw new KeyNotFoundException($"Supplier '{supplierId}' was not found.");
    }
}
