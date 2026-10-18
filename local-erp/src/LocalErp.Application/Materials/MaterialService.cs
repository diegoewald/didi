using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Domain.Materials;
using LocalErp.Domain.Security;

namespace LocalErp.Application.Materials;

public sealed class MaterialService(
    IMaterialRepository repository,
    IAuthorizationService authorizationService)
{
    private readonly IMaterialRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));

    public async Task<MaterialResult> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.MaterialsManage, cancellationToken);
        var material = new Material(name);
        await _repository.AddAsync(material, cancellationToken);
        return MaterialResult.FromDomain(material);
    }

    public async Task<IReadOnlyList<MaterialResult>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.MaterialsRead, cancellationToken);
        var materials = await _repository.ListAsync(includeInactive, cancellationToken);
        return materials.Select(MaterialResult.FromDomain).ToArray();
    }

    public async Task<MaterialResult> RenameAsync(
        Guid materialId,
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.MaterialsManage, cancellationToken);
        var material = await GetRequiredAsync(materialId, cancellationToken);
        material.Rename(name);
        await _repository.UpdateAsync(material, cancellationToken);
        return MaterialResult.FromDomain(material);
    }

    public async Task<MaterialResult> SetActiveAsync(
        Guid materialId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.MaterialsManage, cancellationToken);
        var material = await GetRequiredAsync(materialId, cancellationToken);

        if (isActive)
        {
            material.Activate();
        }
        else
        {
            material.Deactivate();
        }

        await _repository.UpdateAsync(material, cancellationToken);
        return MaterialResult.FromDomain(material);
    }

    private async Task<Material> GetRequiredAsync(Guid materialId, CancellationToken cancellationToken)
    {
        if (materialId == Guid.Empty)
        {
            throw new ArgumentException("Material identifier cannot be empty.", nameof(materialId));
        }

        return await _repository.GetByIdAsync(materialId, cancellationToken)
            ?? throw new KeyNotFoundException($"Material '{materialId}' was not found.");
    }
}
