using LocalErp.Domain.Companies;

namespace LocalErp.Application.Companies;

public sealed record CompanyBranchResult(Guid Id, Guid CompanyId, string Name, bool IsActive)
{
    public static CompanyBranchResult FromDomain(CompanyBranch branch)
    {
        ArgumentNullException.ThrowIfNull(branch);
        return new CompanyBranchResult(branch.Id, branch.CompanyId, branch.Name, branch.IsActive);
    }
}
