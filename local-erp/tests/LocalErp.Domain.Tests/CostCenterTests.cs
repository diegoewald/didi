using LocalErp.Domain.CostCenters;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class CostCenterTests
{
    [TestMethod]
    public void ConstructorNormalizesNameAndStartsActive()
    {
        var costCenter = new CostCenter("  Produção  ");

        Assert.AreEqual("Produção", costCenter.Name);
        Assert.IsTrue(costCenter.IsActive);
    }

    [TestMethod]
    public void ConstructorRejectsBlankName()
    {
        Assert.Throws<ArgumentException>(() => new CostCenter("   "));
    }

    [TestMethod]
    public void RenameRejectsNameLongerThanSupportedLimit()
    {
        var costCenter = new CostCenter("Produção");
        var tooLong = new string('A', CostCenter.NameMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => costCenter.Rename(tooLong));
    }

    [TestMethod]
    public void CostCenterCanBeDeactivatedAndReactivated()
    {
        var costCenter = new CostCenter("Produção");

        costCenter.Deactivate();
        Assert.IsFalse(costCenter.IsActive);

        costCenter.Activate();
        Assert.IsTrue(costCenter.IsActive);
    }
}
