using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Domain.Purchasing;
using LocalErp.Domain.Security;

namespace LocalErp.Application.Purchasing;

public sealed record RequestForQuotationPurchaseRequestLineOption(
    Guid Id,
    int LineNumber,
    string Description,
    decimal Quantity,
    Guid MeasurementUnitId,
    string MeasurementUnitName,
    DateOnly? NeededByDate);

public sealed record RequestForQuotationPurchaseRequestOption(
    Guid Id,
    PurchaseRequestStatus Status,
    DateTime CreatedAtUtc,
    string? Notes,
    IReadOnlyList<RequestForQuotationPurchaseRequestLineOption> Lines);

public sealed record RequestForQuotationSupplierOption(Guid Id, string Name, bool IsActive);

public sealed class RequestForQuotationLookupService(
    IPurchaseRequestRepository purchaseRequestRepository,
    IMeasurementUnitRepository measurementUnitRepository,
    ISupplierRepository supplierRepository,
    IAuthorizationService authorizationService)
{
    private readonly IPurchaseRequestRepository _purchaseRequestRepository = purchaseRequestRepository
        ?? throw new ArgumentNullException(nameof(purchaseRequestRepository));
    private readonly IMeasurementUnitRepository _measurementUnitRepository = measurementUnitRepository
        ?? throw new ArgumentNullException(nameof(measurementUnitRepository));
    private readonly ISupplierRepository _supplierRepository = supplierRepository
        ?? throw new ArgumentNullException(nameof(supplierRepository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));

    public async Task<IReadOnlyList<RequestForQuotationPurchaseRequestOption>> ListSubmittedPurchaseRequestsAsync(
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var requests = await _purchaseRequestRepository.ListAsync(PurchaseRequestStatus.Submitted, cancellationToken);
        var units = await LoadUnitNamesAsync(cancellationToken);
        return requests
            .OrderByDescending(request => request.CreatedAtUtc)
            .ThenBy(request => request.Id)
            .Select(request => MapPurchaseRequest(request, units))
            .ToArray();
    }

    public async Task<RequestForQuotationPurchaseRequestOption> GetPurchaseRequestAsync(
        Guid purchaseRequestId,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsRead, cancellationToken);
        if (purchaseRequestId == Guid.Empty)
        {
            throw new ArgumentException("Purchase request identifier cannot be empty.", nameof(purchaseRequestId));
        }

        var request = await _purchaseRequestRepository.GetByIdAsync(purchaseRequestId, cancellationToken)
            ?? throw new KeyNotFoundException($"Purchase request '{purchaseRequestId}' was not found.");
        var units = await LoadUnitNamesAsync(cancellationToken);
        return MapPurchaseRequest(request, units);
    }

    public async Task<IReadOnlyList<RequestForQuotationSupplierOption>> ListActiveSuppliersAsync(
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsManage, cancellationToken);
        var suppliers = await _supplierRepository.ListAsync(includeInactive: false, cancellationToken);
        return suppliers
            .Where(supplier => supplier.IsActive)
            .OrderBy(supplier => supplier.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(supplier => supplier.Id)
            .Select(supplier => new RequestForQuotationSupplierOption(supplier.Id, supplier.Name, supplier.IsActive))
            .ToArray();
    }

    public async Task<IReadOnlyList<RequestForQuotationSupplierOption>> GetSupplierReferencesAsync(
        IReadOnlyCollection<Guid> supplierIds,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(supplierIds);
        await _authorizationService.DemandAsync(PermissionCatalog.RequestForQuotationsRead, cancellationToken);

        var result = new List<RequestForQuotationSupplierOption>();
        foreach (var supplierId in supplierIds.Distinct().OrderBy(id => id))
        {
            if (supplierId == Guid.Empty)
            {
                throw new ArgumentException("Supplier identifier cannot be empty.", nameof(supplierIds));
            }

            var supplier = await _supplierRepository.GetByIdAsync(supplierId, cancellationToken)
                ?? throw new KeyNotFoundException($"Supplier '{supplierId}' was not found.");
            result.Add(new RequestForQuotationSupplierOption(supplier.Id, supplier.Name, supplier.IsActive));
        }

        return result;
    }

    private async Task<IReadOnlyDictionary<Guid, string>> LoadUnitNamesAsync(CancellationToken cancellationToken)
    {
        var units = await _measurementUnitRepository.ListAsync(includeInactive: true, cancellationToken);
        return units.ToDictionary(unit => unit.Id, unit => unit.Name);
    }

    private static RequestForQuotationPurchaseRequestOption MapPurchaseRequest(
        PurchaseRequest request,
        IReadOnlyDictionary<Guid, string> unitNames)
    {
        return new RequestForQuotationPurchaseRequestOption(
            request.Id,
            request.Status,
            request.CreatedAtUtc,
            request.Notes,
            request.Lines
                .OrderBy(line => line.LineNumber)
                .Select(line => new RequestForQuotationPurchaseRequestLineOption(
                    line.Id,
                    line.LineNumber,
                    line.Description,
                    line.Quantity,
                    line.MeasurementUnitId,
                    unitNames.TryGetValue(line.MeasurementUnitId, out var unitName)
                        ? unitName
                        : "Unidade não encontrada",
                    line.NeededByDate))
                .ToArray());
    }
}
