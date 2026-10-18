namespace LocalErp.Application.Account;

public sealed class CurrentPasswordInvalidException : InvalidOperationException
{
    public CurrentPasswordInvalidException()
        : base("The current password is invalid.")
    {
    }
}
