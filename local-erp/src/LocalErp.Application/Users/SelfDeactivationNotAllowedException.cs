namespace LocalErp.Application.Users;

public sealed class SelfDeactivationNotAllowedException()
    : InvalidOperationException("The authenticated user cannot deactivate their own account.");
