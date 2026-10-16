using LocalErp.Domain.Companies;
using Microsoft.EntityFrameworkCore;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Persistence.Tests;

[TestClass]
public sealed class ErpDbContextModelTests
{
    [TestMethod]
    public void CompanyMappingDefinesRequiredNameAndStableIdentifier()
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new ErpDbContext(options);
        var entity = context.Model.FindEntityType(typeof(Company))
            ?? throw new AssertFailedException("Company entity mapping was not found.");

        Assert.AreEqual("Companies", entity.GetTableName());

        var name = entity.FindProperty(nameof(Company.Name))
            ?? throw new AssertFailedException("Company.Name mapping was not found.");
        Assert.IsFalse(name.IsNullable);
        Assert.AreEqual(Company.NameMaxLength, name.GetMaxLength());

        var id = entity.FindProperty(nameof(Company.Id))
            ?? throw new AssertFailedException("Company.Id mapping was not found.");
        Assert.AreEqual(Microsoft.EntityFrameworkCore.Metadata.ValueGenerated.Never, id.ValueGenerated);
    }
}
