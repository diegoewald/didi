using LocalErp.Domain.Suppliers;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class SupplierTests
{
    [TestMethod]
    public void ConstructorNormalizesNameAndStartsActive()
    {
        var supplier = new Supplier("  Fornecedor Exemplo  ");

        Assert.AreEqual("Fornecedor Exemplo", supplier.Name);
        Assert.IsTrue(supplier.IsActive);
    }

    [TestMethod]
    public void ConstructorRejectsBlankName()
    {
        Assert.Throws<ArgumentException>(() => new Supplier("   "));
    }

    [TestMethod]
    public void RenameRejectsNameLongerThanSupportedLimit()
    {
        var supplier = new Supplier("Fornecedor Exemplo");
        var tooLong = new string('A', Supplier.NameMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => supplier.Rename(tooLong));
    }

    [TestMethod]
    public void SupplierCanBeDeactivatedAndReactivated()
    {
        var supplier = new Supplier("Fornecedor Exemplo");

        supplier.Deactivate();
        Assert.IsFalse(supplier.IsActive);

        supplier.Activate();
        Assert.IsTrue(supplier.IsActive);
    }
}
