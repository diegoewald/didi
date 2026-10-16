using LocalErp.Domain.Common;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class EntityTests
{
    [TestMethod]
    public void NewEntityGetsNonEmptyId()
    {
        var entity = new TestEntity();

        Assert.AreNotEqual(Guid.Empty, entity.Id);
    }

    private sealed class TestEntity : Entity
    {
    }
}
