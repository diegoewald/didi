using LocalErp.Domain.Materials;

namespace LocalErp.Application.Materials;

public sealed record MaterialResult(Guid Id, string Name, bool IsActive)
{
    public static MaterialResult FromDomain(Material material)
    {
        ArgumentNullException.ThrowIfNull(material);
        return new MaterialResult(material.Id, material.Name, material.IsActive);
    }
}
