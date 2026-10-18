using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Application.Security;
using LocalErp.Domain.Companies;
using LocalErp.Domain.Security;

namespace LocalErp.Application.Companies;

public sealed class CompanyBranchService(
    ICompanyBranchRepository branchRepository,
    ICompanyRepository companyRepository,
    IAuthorizationService authorizationService)
{
    private readonly ICompanyBranchRepository _branchRepository = branchRepository
        ?? throw new ArgumentNullException(nameof(branchRepository));
    private readonly ICompanyRepository _companyRepository = companyRepository
        ?? throw new ArgumentNullException(nameof(companyRepository));
    private readonly IAuthorizationService _authorizationService = authorizationService
        ?? throw new ArgumentNullException(nameof(authorizationService));

    public async Task<CompanyBranchResult> CreateAsync(
        Guid companyId,
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CompaniesManage, cancellationToken);
        await GetCompanyRequiredAsync(companyId, cancellationToken);

        var branch = new CompanyBranch(companyId, name);
        await _branchRepository.AddAsync(branch, cancellationToken);
        return CompanyBranchResult.FromDomain(branch);
    }

    public async Task<IReadOnlyList<CompanyBranchResult>> ListAsync(
        Guid companyId,
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CompaniesRead, cancellationToken);
        await GetCompanyRequiredAsync(companyId, cancellationToken);

        var branches = await _branchRepository.ListByCompanyAsync(
            companyId,
            includeInactive,
            cancellationToken);
        return branches.Select(CompanyBranchResult.FromDomain).ToArray();
    }

    public async Task<CompanyBranchResult> RenameAsync(
        Guid branchId,
        string name,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CompaniesManage, cancellationToken);
        var branch = await GetBranchRequiredAsync(branchId, cancellationToken);
        branch.Rename(name);
        await _branchRepository.UpdateAsync(branch, cancellationToken);
        return CompanyBranchResult.FromDomain(branch);
    }

    public async Task<CompanyBranchResult> SetActiveAsync(
        Guid branchId,
        bool isActive,
        CancellationToken cancellationToken = default)
    {
        await _authorizationService.DemandAsync(PermissionCatalog.CompaniesManage, cancellationToken);
        var branch = await GetBranchRequiredAsync(branchId, cancellationToken);

        if (isActive)
        {
            branch.Activate();
        }
        else
        {
            branch.Deactivate();
        }

        await _branchRepository.UpdateAsync(branch, cancellationToken);
        return CompanyBranchResult.FromDomain(branch);
    }

    private async Task<Company> GetCompanyRequiredAsync(
        Guid companyId,
        CancellationToken cancellationToken)
    {
        if (companyId == Guid.Empty)
        {
            throw new ArgumentException("Company identifier cannot be empty.", nameof(companyId));
        }

        return await _companyRepository.GetByIdAsync(companyId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company '{companyId}' was not found.");
    }

    private async Task<CompanyBranch> GetBranchRequiredAsync(
        Guid branchId,
        CancellationToken cancellationToken)
    {
        if (branchId == Guid.Empty)
        {
            throw new ArgumentException("Company branch identifier cannot be empty.", nameof(branchId));
        }

        return await _branchRepository.GetByIdAsync(branchId, cancellationToken)
            ?? throw new KeyNotFoundException($"Company branch '{branchId}' was not found.");
    }
}
