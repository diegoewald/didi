using LocalErp.Domain.Materials;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class MaterialTests
{
    [TestMethod]
    public void ConstructorNormalizesNameAndStartsActive()
    {
        var material = new Material("  Material Exemplo  ");

        Assert.AreEqual("Material Exemplo", material.Name);
        Assert.IsTrue(material.IsActive);
    }

    [TestMethod]
    public void ConstructorRejectsBlankName()
    {
        Assert.Throws<ArgumentException>(() => new Material("   "));
    }

    [TestMethod]
    public void RenameRejectsNameLongerThanSupportedLimit()
    {
        var material = new Material("Material Exemplo");
        var tooLong = new string('A', Material.NameMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => material.Rename(tooLong));
    }

    [TestMethod]
    public void MaterialCanBeDeactivatedAndReactivated()
    {
        var material = new Material("Material Exemplo");

        material.Deactivate();
        Assert.IsFalse(material.IsActive);

        material.Activate();
        Assert.IsTrue(material.IsActive);
    }
}
