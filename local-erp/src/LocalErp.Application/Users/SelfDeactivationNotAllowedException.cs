namespace LocalErp.Application.Users;

public sealed class SelfDeactivationNotAllowedException : InvalidOperationException
{
    public SelfDeactivationNotAllowedException()
        : base("The authenticated user cannot deactivate their own account.")
    {
    }
}
