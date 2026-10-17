using LocalErp.Domain.Security;

namespace LocalErp.Application.Roles;

public sealed record UserRoleAssignmentResult(
    Guid RoleId,
    string RoleName,
    bool RoleIsActive,
    bool IsAssigned)
{
    public static UserRoleAssignmentResult FromDomain(Role role, bool isAssigned)
    {
        ArgumentNullException.ThrowIfNull(role);
        return new UserRoleAssignmentResult(role.Id, role.Name, role.IsActive, isAssigned);
    }
}
