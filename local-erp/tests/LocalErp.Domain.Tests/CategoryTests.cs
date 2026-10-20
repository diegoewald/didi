using LocalErp.Domain.Categories;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class CategoryTests
{
    [TestMethod]
    public void ConstructorNormalizesNameAndStartsActive()
    {
        var category = new Category("  Matéria-prima  ");

        Assert.AreEqual("Matéria-prima", category.Name);
        Assert.IsTrue(category.IsActive);
    }

    [TestMethod]
    public void ConstructorRejectsBlankName()
    {
        Assert.Throws<ArgumentException>(() => new Category("   "));
    }

    [TestMethod]
    public void RenameRejectsNameLongerThanSupportedLimit()
    {
        var category = new Category("Matéria-prima");
        var tooLong = new string('A', Category.NameMaxLength + 1);

        Assert.Throws<ArgumentOutOfRangeException>(() => category.Rename(tooLong));
    }

    [TestMethod]
    public void CategoryCanBeDeactivatedAndReactivated()
    {
        var category = new Category("Matéria-prima");

        category.Deactivate();
        Assert.IsFalse(category.IsActive);

        category.Activate();
        Assert.IsTrue(category.IsActive);
    }
}
