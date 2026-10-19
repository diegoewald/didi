using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Domain.Security;
using LocalErp.Domain.Units;

namespace LocalErp.Application.Units;

public sealed class MeasurementUnitService(
    IMeasurementUnitRepository repository,
    IAuthorizationService authorizationService)
{
    private readonly IMeasurementUnitRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));

    public async Task<MeasurementUnitResult> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.UnitsManage, cancellationToken);
        var unit = new MeasurementUnit(name);
        await _repository.AddAsync(unit, cancellationToken);
        return MeasurementUnitResult.FromDomain(unit);
    }

    public async Task<IReadOnlyList<MeasurementUnitResult>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.UnitsRead, cancellationToken);
        var units = await _repository.ListAsync(includeInactive, cancellationToken);
        return units.Select(MeasurementUnitResult.FromDomain).ToArray();
    }

    public async Task<MeasurementUnitResult> RenameAsync(
        Guid unitId,
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.UnitsManage, cancellationToken);
        var unit = await GetRequiredAsync(unitId, cancellationToken);
        unit.Rename(name);
        await _repository.UpdateAsync(unit, cancellationToken);
        return MeasurementUnitResult.FromDomain(unit);
    }

    public async Task<MeasurementUnitResult> SetActiveAsync(
        Guid unitId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.UnitsManage, cancellationToken);
        var unit = await GetRequiredAsync(unitId, cancellationToken);

        if (isActive)
        {
            unit.Activate();
        }
        else
        {
            unit.Deactivate();
        }

        await _repository.UpdateAsync(unit, cancellationToken);
        return MeasurementUnitResult.FromDomain(unit);
    }

    private async Task<MeasurementUnit> GetRequiredAsync(
        Guid unitId,
        CancellationToken cancellationToken)
    {
        if (unitId == Guid.Empty)
        {
            throw new ArgumentException("Measurement unit identifier cannot be empty.", nameof(unitId));
        }

        return await _repository.GetByIdAsync(unitId, cancellationToken)
            ?? throw new KeyNotFoundException($"Measurement unit '{unitId}' was not found.");
    }
}
