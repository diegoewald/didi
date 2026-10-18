namespace LocalErp.Domain.Security;

public static class SecurityAuditCodes
{
    public const string AuthenticationSignInSucceeded = "authentication.sign_in.succeeded";
    public const string AuthenticationSignInFailed = "authentication.sign_in.failed";
    public const string AuthenticationSignInBlocked = "authentication.sign_in.blocked";
    public const string AuthenticationThrottleApplied = "authentication.throttle.applied";
    public const string AuthenticationSignOut = "authentication.sign_out";
    public const string AdministratorBootstrapSucceeded = "authentication.bootstrap.succeeded";

    public const string AdministrationUserCreated = "administration.user.created";
    public const string AdministrationUserActivated = "administration.user.activated";
    public const string AdministrationUserDeactivated = "administration.user.deactivated";
    public const string AdministrationUserPasswordReset = "administration.user.password_reset";
    public const string AdministrationUserRoleAssigned = "administration.user_role.assigned";
    public const string AdministrationUserRoleRemoved = "administration.user_role.removed";
    public const string AdministrationRoleCreated = "administration.role.created";
    public const string AdministrationRoleActivated = "administration.role.activated";
    public const string AdministrationRoleDeactivated = "administration.role.deactivated";
    public const string AdministrationRolePermissionGranted = "administration.role_permission.granted";
    public const string AdministrationRolePermissionRevoked = "administration.role_permission.revoked";
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
