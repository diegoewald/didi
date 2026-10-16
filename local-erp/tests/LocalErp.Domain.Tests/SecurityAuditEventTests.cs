using LocalErp.Domain.Security;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace LocalErp.Domain.Tests;

[TestClass]
public sealed class SecurityAuditEventTests
{
    [TestMethod]
    public void ConstructorTrimsCanonicalCodesAndPreservesSafeFields()
    {
        var occurredAtUtc = new DateTime(2026, 10, 16, 14, 30, 0, DateTimeKind.Utc);
        var userId = Guid.NewGuid();

        var auditEvent = new SecurityAuditEvent(
            occurredAtUtc,
            " authentication.sign_in.failed ",
            userId,
            " invalid_password ");

        Assert.AreEqual(occurredAtUtc, auditEvent.OccurredAtUtc);
        Assert.AreEqual("authentication.sign_in.failed", auditEvent.EventCode);
        Assert.AreEqual("invalid_password", auditEvent.DetailCode);
        Assert.AreEqual(userId, auditEvent.UserId);
    }

    [TestMethod]
    public void ConstructorRejectsNonUtcTimestamp()
    {
        var localTime = new DateTime(2026, 10, 16, 14, 30, 0, DateTimeKind.Local);

        Assert.ThrowsExactly<ArgumentException>(
            () => new SecurityAuditEvent(localTime, SecurityAuditCodes.AuthenticationSignInFailed));
    }

    [TestMethod]
    public void ConstructorRejectsNonCanonicalUppercaseCode()
    {
        var occurredAtUtc = new DateTime(2026, 10, 16, 14, 30, 0, DateTimeKind.Utc);

        Assert.ThrowsExactly<ArgumentException>(
            () => new SecurityAuditEvent(occurredAtUtc, "authentication.Sign_In.failed"));
    }

    [TestMethod]
    public void ConstructorRejectsUnsafeCodeCharacters()
    {
        var occurredAtUtc = new DateTime(2026, 10, 16, 14, 30, 0, DateTimeKind.Utc);

        Assert.ThrowsExactly<ArgumentException>(
            () => new SecurityAuditEvent(occurredAtUtc, "authentication\nsign_in"));
    }
}
