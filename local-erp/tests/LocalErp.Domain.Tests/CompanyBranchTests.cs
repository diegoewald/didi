using LocalErp.Domain.Companies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class CompanyBranchTests
{
    [TestMethod]
    public void ConstructorNormalizesNameAndStartsActive()
    {
        var companyId = Guid.NewGuid();

        var branch = new CompanyBranch(companyId, "  Filial Norte  ");

        Assert.AreEqual(companyId, branch.CompanyId);
        Assert.AreEqual("Filial Norte", branch.Name);
        Assert.IsTrue(branch.IsActive);
    }

    [TestMethod]
    public void ConstructorRejectsEmptyCompanyIdentifier()
    {
        Assert.ThrowsExactly<ArgumentException>(
            () => new CompanyBranch(Guid.Empty, "Filial"));
    }

    [TestMethod]
    public void RenameAndStatusChangesPreserveCompanyRelationship()
    {
        var companyId = Guid.NewGuid();
        var branch = new CompanyBranch(companyId, "Filial Antiga");

        branch.Rename("  Filial Nova  ");
        branch.Deactivate();

        Assert.AreEqual(companyId, branch.CompanyId);
        Assert.AreEqual("Filial Nova", branch.Name);
        Assert.IsFalse(branch.IsActive);

        branch.Activate();
        Assert.IsTrue(branch.IsActive);
    }
}
