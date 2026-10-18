namespace LocalErp.Domain.Security;

public static class PermissionCatalog
{
    public const int CodeMaxLength = 100;

    public const string CompaniesRead = "companies.read";
    public const string CompaniesManage = "companies.manage";
    public const string SuppliersRead = "suppliers.read";
    public const string SuppliersManage = "suppliers.manage";
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string RolesRead = "roles.read";
    public const string RolesManage = "roles.manage";

    private static readonly HashSet<string> KnownPermissions =
    [
        CompaniesRead,
        CompaniesManage,
        SuppliersRead,
        SuppliersManage,
        UsersRead,
        UsersManage,
        RolesRead,
        RolesManage,
    ];

    public static IReadOnlySet<string> All { get; } = KnownPermissions;

    public static string Validate(string permissionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);

        var normalized = permissionCode.Trim().ToLowerInvariant();
        if (normalized.Length > CodeMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(permissionCode),
                permissionCode,
                $"Permission code cannot exceed {CodeMaxLength} characters.");
        }

        if (!KnownPermissions.Contains(normalized))
        {
            throw new ArgumentOutOfRangeException(
                nameof(permissionCode),
                permissionCode,
                "Permission code is not registered in the application catalog.");
        }

        return normalized;
    }
}
