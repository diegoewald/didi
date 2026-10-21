using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Domain.CostCenters;
using LocalErp.Domain.Security;

namespace LocalErp.Application.CostCenters;

public sealed class CostCenterService(
    ICostCenterRepository repository,
    IAuthorizationService authorizationService)
{
    private readonly ICostCenterRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));

    public async Task<CostCenterResult> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CostCentersManage, cancellationToken);
        var costCenter = new CostCenter(name);
        await _repository.AddAsync(costCenter, cancellationToken);
        return CostCenterResult.FromDomain(costCenter);
    }

    public async Task<IReadOnlyList<CostCenterResult>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CostCentersRead, cancellationToken);
        var costCenters = await _repository.ListAsync(includeInactive, cancellationToken);
        return costCenters.Select(CostCenterResult.FromDomain).ToArray();
    }

    public async Task<CostCenterResult> RenameAsync(
        Guid costCenterId,
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CostCentersManage, cancellationToken);
        var costCenter = await GetRequiredAsync(costCenterId, cancellationToken);
        costCenter.Rename(name);
        await _repository.UpdateAsync(costCenter, cancellationToken);
        return CostCenterResult.FromDomain(costCenter);
    }

    public async Task<CostCenterResult> SetActiveAsync(
        Guid costCenterId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CostCentersManage, cancellationToken);
        var costCenter = await GetRequiredAsync(costCenterId, cancellationToken);

        if (isActive)
        {
            costCenter.Activate();
        }
        else
        {
            costCenter.Deactivate();
        }

        await _repository.UpdateAsync(costCenter, cancellationToken);
        return CostCenterResult.FromDomain(costCenter);
    }

    private async Task<CostCenter> GetRequiredAsync(
        Guid costCenterId,
        CancellationToken cancellationToken)
    {
        if (costCenterId == Guid.Empty)
        {
            throw new ArgumentException("Cost center identifier cannot be empty.", nameof(costCenterId));
        }

        return await _repository.GetByIdAsync(costCenterId, cancellationToken)
            ?? throw new KeyNotFoundException($"Cost center '{costCenterId}' was not found.");
    }
}
