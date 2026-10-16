using LocalErp.Domain.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class RoleTests
{
    [TestMethod]
    public void ConstructorNormalizesNameAndStartsActive()
    {
        var role = new Role("  Operadores  ");

        Assert.AreEqual("Operadores", role.Name);
        Assert.AreEqual("OPERADORES", role.NormalizedName);
        Assert.IsTrue(role.IsActive);
    }

    [TestMethod]
    public void NormalizeNameUsesUnicodeCompatibilityNormalization()
    {
        var normalized = Role.NormalizeName("Ａdmin");

        Assert.AreEqual("ADMIN", normalized);
    }

    [TestMethod]
    public void PermissionCatalogRejectsUnknownPermission()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => PermissionCatalog.Validate("unknown.permission"));
    }

    [TestMethod]
    public void PermissionCatalogNormalizesKnownPermission()
    {
        var permission = PermissionCatalog.Validate("  USERS.MANAGE  ");

        Assert.AreEqual(PermissionCatalog.UsersManage, permission);
    }
}
