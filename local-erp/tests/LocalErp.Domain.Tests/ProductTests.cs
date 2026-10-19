using LocalErp.Domain.Products;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class ProductTests
{
    [TestMethod]
    public void ConstructorNormalizesNameAndStartsActive()
    {
        var product = new Product("  Produto Exemplo  ");

        Assert.AreEqual("Produto Exemplo", product.Name);
        Assert.IsTrue(product.IsActive);
    }

    [TestMethod]
    public void ConstructorRejectsBlankName()
    {
        Assert.Throws<ArgumentException>(() => new Product("   "));
    }

    [TestMethod]
    public void RenameRejectsNameLongerThanSupportedLimit()
    {
        var product = new Product("Produto Exemplo");
        var tooLong = new string('A', Product.NameMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => product.Rename(tooLong));
    }

    [TestMethod]
    public void ProductCanBeDeactivatedAndReactivated()
    {
        var product = new Product("Produto Exemplo");

        product.Deactivate();
        Assert.IsFalse(product.IsActive);

        product.Activate();
        Assert.IsTrue(product.IsActive);
    }
}
