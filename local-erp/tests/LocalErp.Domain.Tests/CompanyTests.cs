using LocalErp.Domain.Companies;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class CompanyTests
{
    [TestMethod]
    public void ConstructorNormalizesNameAndStartsActive()
    {
        var company = new Company("  Empresa Exemplo  ");

        Assert.AreEqual("Empresa Exemplo", company.Name);
        Assert.IsTrue(company.IsActive);
    }

    [TestMethod]
    public void ConstructorRejectsBlankName()
    {
        Assert.Throws<ArgumentException>(() => new Company("   "));
    }

    [TestMethod]
    public void RenameRejectsNameLongerThanSupportedLimit()
    {
        var company = new Company("Empresa Exemplo");
        var tooLong = new string('A', Company.NameMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => company.Rename(tooLong));
    }

    [TestMethod]
    public void CompanyCanBeDeactivatedAndReactivated()
    {
        var company = new Company("Empresa Exemplo");

        company.Deactivate();
        Assert.IsFalse(company.IsActive);

        company.Activate();
        Assert.IsTrue(company.IsActive);
    }
}
