using LocalErp.Domain.Security;

namespace LocalErp.Application.Roles;

public sealed record RoleResult(
    Guid Id,
    string Name,
    bool IsActive,
    IReadOnlyList<string> Permissions)
{
    public static RoleResult FromDomain(Role role, IReadOnlyList<string> permissions)
    {
        ArgumentNullException.ThrowIfNull(role);
        ArgumentNullException.ThrowIfNull(permissions);
        return new RoleResult(role.Id, role.Name, role.IsActive, permissions);
    }
}
