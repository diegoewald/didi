using LocalErp.Application.Users;

namespace LocalErp.Application.Security;

public sealed record AuthenticationResult(bool Succeeded, UserResult? User)
{
    public static AuthenticationResult Failed { get; } = new(false, null);

    public static AuthenticationResult Success(UserResult user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new AuthenticationResult(true, user);
    }
}
