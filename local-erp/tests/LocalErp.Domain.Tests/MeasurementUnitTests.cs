using LocalErp.Domain.Units;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class MeasurementUnitTests
{
    [TestMethod]
    public void ConstructorNormalizesNameAndStartsActive()
    {
        var unit = new MeasurementUnit("  Quilograma  ");

        Assert.AreEqual("Quilograma", unit.Name);
        Assert.IsTrue(unit.IsActive);
    }

    [TestMethod]
    public void ConstructorRejectsBlankName()
    {
        Assert.Throws<ArgumentException>(() => new MeasurementUnit("   "));
    }

    [TestMethod]
    public void RenameRejectsNameLongerThanSupportedLimit()
    {
        var unit = new MeasurementUnit("Quilograma");
        var tooLong = new string('A', MeasurementUnit.NameMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => unit.Rename(tooLong));
    }

    [TestMethod]
    public void MeasurementUnitCanBeDeactivatedAndReactivated()
    {
        var unit = new MeasurementUnit("Quilograma");

        unit.Deactivate();
        Assert.IsFalse(unit.IsActive);

        unit.Activate();
        Assert.IsTrue(unit.IsActive);
    }
}
