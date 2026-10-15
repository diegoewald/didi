using LocalErp.Domain.Users;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class UserTests
{
    [TestMethod]
    public void ConstructorNormalizesUsernameAndStartsActive()
    {
        var user = new User("  Diego  ", "hash-value");

        Assert.AreEqual("Diego", user.Username);
        Assert.AreEqual("DIEGO", user.NormalizedUsername);
        Assert.AreEqual("hash-value", user.PasswordHash);
        Assert.IsTrue(user.IsActive);
    }

    [TestMethod]
    public void NormalizeUsernameUsesCompatibilityUnicodeNormalization()
    {
        var normalized = User.NormalizeUsername("  ｄｉｅｇｏ  ");

        Assert.AreEqual("DIEGO", normalized);
    }

    [TestMethod]
    public void PasswordHashCanChangeWithoutExposingPlaintextBehavior()
    {
        var user = new User("diego", "old-hash");

        user.ChangePasswordHash("new-hash");

        Assert.AreEqual("new-hash", user.PasswordHash);
    }

    [TestMethod]
    public void UserCanBeDeactivatedAndReactivated()
    {
        var user = new User("diego", "hash");

        user.Deactivate();
        Assert.IsFalse(user.IsActive);

        user.Activate();
        Assert.IsTrue(user.IsActive);
    }
}
