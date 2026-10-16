using LocalErp.Domain.Companies;

namespace LocalErp.Application.Companies;

public sealed record CompanyResult(Guid Id, string Name, bool IsActive)
{
    public static CompanyResult FromDomain(Company company)
    {
        ArgumentNullException.ThrowIfNull(company);
        return new CompanyResult(company.Id, company.Name, company.IsActive);
    }
}
