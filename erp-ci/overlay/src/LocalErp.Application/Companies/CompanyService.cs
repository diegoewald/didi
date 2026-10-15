using LocalErp.Application.Abstractions.Persistence;
using LocalErp.Domain.Companies;

namespace LocalErp.Application.Companies;

public sealed class CompanyService(ICompanyRepository repository)
{
    private readonly ICompanyRepository _repository = repository
        ?? throw new ArgumentNullException(nameof(repository));

    public async Task<CompanyResult> CreateAsync(
        string name,
        CancellationToken cancellationToken = default)
    {
        var company = new Company(name);
        await _repository.AddAsync(company, cancellationToken);
        return CompanyResult.FromDomain(company);
    }

    public async Task<IReadOnlyList<CompanyResult>> ListAsync(
        bool includeInactive = false,
        CancellationToken cancellationToken = default)
    {
        var companies = await _repository.ListAsync(includeInactive, cancellationToken);
        return companies.Select(CompanyResult.FromDomain).ToArray();
    }

    public async Task<CompanyResult> RenameAsync(
        Guid companyId,
        string name,
        CancellationToken cancellationToken = default)
    {
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
