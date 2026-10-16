using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Companies;
using LocalErp.Application.Security;
using LocalErp.Domain.Security;

namespace LocalErp.Application.Companies;

public sealed class CompanyService(
    ICompanyRepository repository,
    IAuthorizationService authorizationService)
{
    private readonly ICompanyRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));

    public async Task<CompanyResult> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CompaniesManage, cancellationToken);
        var company = new Company(name);
        await _repository.AddAsync(company, cancellationToken);
        return CompanyResult.FromDomain(company);
    }

    public async Task<IReadOnlyList<CompanyResult>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CompaniesRead, cancellationToken);
        var companies = await _repository.ListAsync(includeInactive, cancellationToken);
        return companies.Select(CompanyResult.FromDomain).ToArray();
    }

    public async Task<CompanyResult> RenameAsync(
        Guid companyId,
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CompaniesManage, cancellationToken);
        var company = await GetRequiredAsync(companyId, cancellationToken);
        company.Rename(name);
        await _repository.UpdateAsync(company, cancellationToken);
        return CompanyResult.FromDomain(company);
    }

    public async Task<CompanyResult> SetActiveAsync(
        Guid companyId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CompaniesManage, cancellationToken);
        var company = await GetRequiredAsync(companyId, cancellationToken);

        if (isActive)
        {
            company.Activate();
        }
        else
        {
            company.Deactivate();
        }

        await _repository.UpdateAsync(company, cancellationToken);
        return CompanyResult.FromDomain(company);
    }

    private async Task<Company> GetRequiredAsync(Guid companyId, CancellationToken cancellationToken)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("Company identifier cannot be empty.", nameof(companyId));
        }

        return await _repository.GetByIdAsync(companyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company '{companyId}' was not found.");
    }
}
