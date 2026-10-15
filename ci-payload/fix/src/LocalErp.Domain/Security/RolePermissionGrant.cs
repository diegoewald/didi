namespace LocalErp.Domain.Security;

public sealed class RolePermissionGrant
{
    private RolePermissionGrant()
    {
        PermissionCode = string.Empty;
    }

    public RolePermissionGrant(Guid roleId, string permissionCode)
    {
        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role identifier cannot be empty.", nameof(roleId));
        }

        RoleId = roleId;
        PermissionCode = PermissionCatalog.Validate(permissionCode);
    }

    public Guid RoleId { get; private set; }

    public string PermissionCode { get; private set; } = string.Empty;
}
