using LocalErp.Domain.Units;

namespace LocalErp.Application.Units;

public sealed record MeasurementUnitResult(Guid Id, string Name, bool IsActive)
{
    public static MeasurementUnitResult FromDomain(MeasurementUnit unit)
    {
        ArgumentNullException.ThrowIfNull(unit);
        return new MeasurementUnitResult(unit.Id, unit.Name, unit.IsActive);
    }
}
