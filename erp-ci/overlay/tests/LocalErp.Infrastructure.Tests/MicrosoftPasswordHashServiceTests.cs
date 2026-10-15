using LocalErp.Application.Security;
using LocalErp.Infrastructure.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Infrastructure.Tests;

[TestClass]
public sealed class MicrosoftPasswordHashServiceTests
{
    [TestMethod]
    public void HashAndVerifySucceedsForCorrectPassword()
    {
        var service = new MicrosoftPasswordHashService();

        var hash = service.Hash("correct horse battery staple");
        var result = service.Verify(hash, "correct horse battery staple");

        Assert.AreEqual(PasswordVerificationStatus.Success, result);
        Assert.AreNotEqual("correct horse battery staple", hash);
    }

    [TestMethod]
    public void VerifyFailsForWrongPassword()
    {
        var service = new MicrosoftPasswordHashService();
        var hash = service.Hash("expected password");

        var result = service.Verify(hash, "wrong password");

        Assert.AreEqual(PasswordVerificationStatus.Failed, result);
    }

    [TestMethod]
    public void VerifyTreatsMalformedHashAsFailure()
    {
        var service = new MicrosoftPasswordHashService();

        var result = service.Verify("not valid base64 !", "password");

        Assert.AreEqual(PasswordVerificationStatus.Failed, result);
    }

    [TestMethod]
    public void VerifySignalsRehashWhenStoredCostIsLower()
    {
        var legacyService = new MicrosoftPasswordHashService(MicrosoftPasswordHashService.MinimumIterationCount);
        var strongerService = new MicrosoftPasswordHashService(MicrosoftPasswordHashService.MinimumIterationCount + 10_000);
        var hash = legacyService.Hash("password to upgrade");

        var result = strongerService.Verify(hash, "password to upgrade");

        Assert.AreEqual(PasswordVerificationStatus.SuccessRehashNeeded, result);
    }

    [TestMethod]
    public void ConstructorRejectsCostBelowSecurityBaseline()
    {
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(
            () => new MicrosoftPasswordHashService(MicrosoftPasswordHashService.MinimumIterationCount - 1));
    }
}
