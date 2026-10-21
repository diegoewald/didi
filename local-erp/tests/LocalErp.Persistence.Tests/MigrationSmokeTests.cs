using System.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class MigrationSmokeTests
{
    private const string InitialCreateMigration = "20261015121025_InitialCreate";
    private const string AddLocalUsersMigration = "20261015130329_AddLocalUsers";
    private const string AddRbacAuthorizationMigration = "20261015141238_AddRbacAuthorization";
    private const string AddAuthenticationThrottlingMigration = "20261016131952_AddAuthenticationThrottling";
    private const string AddSecurityAuditTrailMigration = "20261016135247_AddSecurityAuditTrail";
    private const string AddAdministrativeAuditContextMigration = "20261017234500_AddAdministrativeAuditContext";
    private const string AddCompanyBranchesMigration = "20261018105000_AddCompanyBranches";
    private const string AddSuppliersMigration = "20261018142500_AddSuppliers";
    private const string AddMaterialsMigration = "20261018222000_AddMaterials";
    private const string AddProductsMigration = "20261019005000_AddProducts";
    private const string AddMeasurementUnitsMigration = "20261019093000_AddMeasurementUnits";
    private const string AddCategoriesMigration = "20261020113000_AddCategories";
    private const string AddCostCentersMigration = "20261021100000_AddCostCenters";
    private const string AddPurchaseRequestsMigration = "20261021123000_AddPurchaseRequests";

    [TestMethod]
    public void MigrationsCanApplyRollbackAndReapply()
    {
        var testDirectory = Path.Combine(Path.GetTempPath(), "LocalErp", "Tests", Guid.NewGuid().ToString("N"));
        var databasePath = Path.Combine(testDirectory, "migration-smoke.db");
        Directory.CreateDirectory(testDirectory);

        try
        {
            var options = new DbContextOptionsBuilder<ErpDbContext>()
                .UseSqlite($"{SqliteConnectionStringFactory.Create(databasePath)};Pooling=False")
                .Options;

            using (var context = new ErpDbContext(options))
            {
                context.Database.Migrate();
                AssertExpectedMigrationsApplied(context);
                AssertTableExists(context, "Categories", expected: true);
                AssertTableExists(context, "Companies", expected: true);
                AssertTableExists(context, "CompanyBranches", expected: true);
                AssertTableExists(context, "CostCenters", expected: true);
                AssertTableExists(context, "Materials", expected: true);
                AssertTableExists(context, "MeasurementUnits", expected: true);
                AssertTableExists(context, "Products", expected: true);
                AssertTableExists(context, "PurchaseRequests", expected: true);
                AssertTableExists(context, "PurchaseRequestLines", expected: true);
                AssertTableExists(context, "Suppliers", expected: true);
                AssertTableExists(context, "Users", expected: true);
                AssertRbacTablesExist(context, expected: true);
                AssertColumnExists(context, "Users", "FailedSignInCount", expected: true);
                AssertColumnExists(context, "Users", "SignInBlockedUntilUtc", expected: true);
                AssertTableExists(context, "SecurityAuditEvents", expected: true);
                AssertColumnExists(context, "SecurityAuditEvents", "ActorUserId", expected: true);
                AssertColumnExists(context, "SecurityAuditEvents", "RoleId", expected: true);

                var migrator = context.Database.GetService<IMigrator>();
                migrator.Migrate(Migration.InitialDatabase);
                AssertTableExists(context, "Categories", expected: false);
                AssertTableExists(context, "Companies", expected: false);
                AssertTableExists(context, "CompanyBranches", expected: false);
                AssertTableExists(context, "CostCenters", expected: false);
                AssertTableExists(context, "Materials", expected: false);
                AssertTableExists(context, "MeasurementUnits", expected: false);
                AssertTableExists(context, "Products", expected: false);
                AssertTableExists(context, "PurchaseRequests", expected: false);
                AssertTableExists(context, "PurchaseRequestLines", expected: false);
                AssertTableExists(context, "Suppliers", expected: false);
                AssertTableExists(context, "Users", expected: false);
                AssertRbacTablesExist(context, expected: false);
                AssertTableExists(context, "SecurityAuditEvents", expected: false);

                migrator.Migrate();
                AssertExpectedMigrationsApplied(context);
                AssertTableExists(context, "Categories", expected: true);
                AssertTableExists(context, "Companies", expected: true);
                AssertTableExists(context, "CompanyBranches", expected: true);
                AssertTableExists(context, "CostCenters", expected: true);
                AssertTableExists(context, "Materials", expected: true);
                AssertTableExists(context, "MeasurementUnits", expected: true);
                AssertTableExists(context, "Products", expected: true);
                AssertTableExists(context, "PurchaseRequests", expected: true);
                AssertTableExists(context, "PurchaseRequestLines", expected: true);
                AssertTableExists(context, "Suppliers", expected: true);
                AssertTableExists(context, "Users", expected: true);
                AssertRbacTablesExist(context, expected: true);
                AssertColumnExists(context, "Users", "FailedSignInCount", expected: true);
                AssertColumnExists(context, "Users", "SignInBlockedUntilUtc", expected: true);
                AssertTableExists(context, "SecurityAuditEvents", expected: true);
                AssertColumnExists(context, "SecurityAuditEvents", "ActorUserId", expected: true);
                AssertColumnExists(context, "SecurityAuditEvents", "RoleId", expected: true);
            }
        }
        finally
        {
            if (Directory.Exists(testDirectory))
            {
                Directory.Delete(testDirectory, recursive: true);
            }
        }
    }

    private static void AssertExpectedMigrationsApplied(ErpDbContext context)
    {
        var appliedMigrations = context.Database.GetAppliedMigrations().ToList();
        CollectionAssert.Contains(appliedMigrations, InitialCreateMigration);
        CollectionAssert.Contains(appliedMigrations, AddLocalUsersMigration);
        CollectionAssert.Contains(appliedMigrations, AddRbacAuthorizationMigration);
        CollectionAssert.Contains(appliedMigrations, AddAuthenticationThrottlingMigration);
        CollectionAssert.Contains(appliedMigrations, AddSecurityAuditTrailMigration);
        CollectionAssert.Contains(appliedMigrations, AddAdministrativeAuditContextMigration);
        CollectionAssert.Contains(appliedMigrations, AddCompanyBranchesMigration);
        CollectionAssert.Contains(appliedMigrations, AddSuppliersMigration);
        CollectionAssert.Contains(appliedMigrations, AddMaterialsMigration);
        CollectionAssert.Contains(appliedMigrations, AddProductsMigration);
        CollectionAssert.Contains(appliedMigrations, AddMeasurementUnitsMigration);
        CollectionAssert.Contains(appliedMigrations, AddCategoriesMigration);
        CollectionAssert.Contains(appliedMigrations, AddCostCentersMigration);
        CollectionAssert.Contains(appliedMigrations, AddPurchaseRequestsMigration);
    }

    private static void AssertRbacTablesExist(ErpDbContext context, bool expected)
    {
        AssertTableExists(context, "Roles", expected);
        AssertTableExists(context, "RolePermissions", expected);
        AssertTableExists(context, "UserRoles", expected);
    }

    private static void AssertTableExists(ErpDbContext context, string tableName, bool expected)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = "SELECT COUNT(*) FROM sqlite_master WHERE type = 'table' AND name = $tableName;";
            var parameter = command.CreateParameter();
            parameter.ParameterName = "$tableName";
            parameter.Value = tableName;
            command.Parameters.Add(parameter);

            var count = Convert.ToInt32(command.ExecuteScalar(), System.Globalization.CultureInfo.InvariantCulture);
            Assert.AreEqual(expected ? 1 : 0, count);
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }

    private static void AssertColumnExists(
        ErpDbContext context,
        string tableName,
        string columnName,
        bool expected)
    {
        var connection = context.Database.GetDbConnection();
        var shouldClose = connection.State != ConnectionState.Open;

        if (shouldClose)
        {
            connection.Open();
        }

        try
        {
            using var command = connection.CreateCommand();
            command.CommandText = $"PRAGMA table_info(\"{tableName}\");";
            using var reader = command.ExecuteReader();
            var found = false;
            while (reader.Read())
            {
                if (string.Equals(reader.GetString(1), columnName, StringComparison.Ordinal))
                {
                    found = true;
                    break;
                }
            }

            Assert.AreEqual(expected, found);
        }
        finally
        {
            if (shouldClose)
            {
                connection.Close();
            }
        }
    }
}
