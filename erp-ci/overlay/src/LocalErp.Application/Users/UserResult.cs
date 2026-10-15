using LocalErp.Domain.Users;

namespace LocalErp.Application.Users;

public sealed record UserResult(Guid Id, string Username, bool IsActive)
{
    public static UserResult FromDomain(User user)
    {
        ArgumentNullException.ThrowIfNull(user);
        return new UserResult(user.Id, user.Username, user.IsActive);
    }
}
