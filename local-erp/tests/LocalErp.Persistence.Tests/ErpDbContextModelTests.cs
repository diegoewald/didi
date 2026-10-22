using LocalErp.Domain.Companies;
using LocalErp.Domain.Purchasing;
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

    [TestMethod]
    public void PurchaseOrderMappingUsesScaledIntegerPriceAndRequiredUniqueness()
    {
        var options = new DbContextOptionsBuilder<ErpDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;

        using var context = new ErpDbContext(options);
        var order = context.Model.FindEntityType(typeof(PurchaseOrder))
            ?? throw new AssertFailedException("PurchaseOrder entity mapping was not found.");
        var line = context.Model.FindEntityType(typeof(PurchaseOrderLine))
            ?? throw new AssertFailedException("PurchaseOrderLine entity mapping was not found.");

        Assert.AreEqual("PurchaseOrders", order.GetTableName());
        Assert.IsTrue(order.GetIndexes().Any(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(
                new[] { nameof(PurchaseOrder.SequenceNumber) })));
        Assert.IsTrue(order.GetIndexes().Any(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(
                new[]
                {
                    nameof(PurchaseOrder.RequestForQuotationAwardId),
                    nameof(PurchaseOrder.SupplierId),
                })));

        var unitPrice = line.FindProperty(nameof(PurchaseOrderLine.UnitPrice))
            ?? throw new AssertFailedException("PurchaseOrderLine.UnitPrice mapping was not found.");
        Assert.AreEqual("INTEGER", unitPrice.GetColumnType());
        Assert.IsNotNull(unitPrice.GetValueConverter());
        Assert.IsTrue(line.GetIndexes().Any(index =>
            index.IsUnique &&
            index.Properties.Select(property => property.Name).SequenceEqual(
                new[] { nameof(PurchaseOrderLine.RequestForQuotationAwardLineId) })));
    }
}
