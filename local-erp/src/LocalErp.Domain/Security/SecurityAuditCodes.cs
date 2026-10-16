namespace LocalErp.Domain.Security;

public static class SecurityAuditCodes
{
    public const string AuthenticationSignInSucceeded = "authentication.sign_in.succeeded";
    public const string AuthenticationSignInFailed = "authentication.sign_in.failed";
    public const string AuthenticationSignInBlocked = "authentication.sign_in.blocked";
    public const string AuthenticationThrottleApplied = "authentication.throttle.applied";
    public const string AuthenticationSignOut = "authentication.sign_out";
    public const string AdministratorBootstrapSucceeded = "authentication.bootstrap.succeeded";
}

public static class SecurityAuditDetailCodes
{
    public const string UnknownAccount = "unknown_account";
    public const string InactiveAccount = "inactive_account";
    public const string InvalidPassword = "invalid_password";
    public const string ProgressiveDelay = "progressive_delay";
    public const string TemporaryLockout = "temporary_lockout";
    public const string TemporarilyBlocked = "temporarily_blocked";
}
