namespace LocalErp.Domain.Security;

public static class PermissionCatalog
{
    public const int CodeMaxLength = 100;

    public const string CategoriesRead = "categories.read";
    public const string CategoriesManage = "categories.manage";
    public const string CompaniesRead = "companies.read";
    public const string CompaniesManage = "companies.manage";
    public const string CostCentersRead = "cost-centers.read";
    public const string CostCentersManage = "cost-centers.manage";
    public const string MaterialsRead = "materials.read";
    public const string MaterialsManage = "materials.manage";
    public const string ProductsRead = "products.read";
    public const string ProductsManage = "products.manage";
    public const string PurchaseRequestsRead = "purchase-requests.read";
    public const string PurchaseRequestsManage = "purchase-requests.manage";
    public const string RequestForQuotationsRead = "request-for-quotations.read";
    public const string RequestForQuotationsManage = "request-for-quotations.manage";
    public const string RequestForQuotationsAward = "request-for-quotations.award";
    public const string SuppliersRead = "suppliers.read";
    public const string SuppliersManage = "suppliers.manage";
    public const string UnitsRead = "units.read";
    public const string UnitsManage = "units.manage";
    public const string UsersRead = "users.read";
    public const string UsersManage = "users.manage";
    public const string RolesRead = "roles.read";
    public const string RolesManage = "roles.manage";

    private static readonly HashSet<string> KnownPermissions =
    [
        CategoriesRead,
        CategoriesManage,
        CompaniesRead,
        CompaniesManage,
        CostCentersRead,
        CostCentersManage,
        MaterialsRead,
        MaterialsManage,
        ProductsRead,
        ProductsManage,
        PurchaseRequestsRead,
        PurchaseRequestsManage,
        RequestForQuotationsRead,
        RequestForQuotationsManage,
        RequestForQuotationsAward,
        SuppliersRead,
        SuppliersManage,
        UnitsRead,
        UnitsManage,
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
