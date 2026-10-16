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
        Assert.AreEqual(0, user.FailedSignInCount);
        Assert.IsNull(user.SignInBlockedUntilUtc);
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

    [TestMethod]
    public void FailedSignInStateCanBeRecordedAndReset()
    {
        var user = new User("diego", "hash");
        var now = new DateTime(2026, 10, 16, 13, 0, 0, DateTimeKind.Utc);

        user.RecordFailedSignIn(now, TimeSpan.FromSeconds(10));

        Assert.AreEqual(1, user.FailedSignInCount);
        Assert.AreEqual(now.AddSeconds(10), user.SignInBlockedUntilUtc);
        Assert.IsTrue(user.IsSignInBlocked(now));
        Assert.IsFalse(user.IsSignInBlocked(now.AddSeconds(11)));

        user.ResetSignInFailures();

        Assert.AreEqual(0, user.FailedSignInCount);
        Assert.IsNull(user.SignInBlockedUntilUtc);
    }

    [TestMethod]
    public void FailedSignInRequiresUtcTimestamp()
    {
        var user = new User("diego", "hash");

        Assert.ThrowsExactly<ArgumentException>(
            () => user.RecordFailedSignIn(DateTime.Now, TimeSpan.Zero));
    }
}
