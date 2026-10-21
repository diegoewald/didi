using LocalErp.Domain.CostCenters;

namespace LocalErp.Application.CostCenters;

public sealed record CostCenterResult(Guid Id, string Name, bool IsActive)
{
    public static CostCenterResult FromDomain(CostCenter costCenter)
    {
        ArgumentNullException.ThrowIfNull(costCenter);
        return new CostCenterResult(costCenter.Id, costCenter.Name, costCenter.IsActive);
    }
}
