using LocalErp.Application.Security;
using LocalErp.Application.Users;

namespace LocalErp.Infrastructure.Security;

public sealed class InMemoryUserSession : IUserSession
{
    private readonly object _sync = new();
    private UserResult? _currentUser;

    public bool IsAuthenticated => CurrentUser is not null;

    public UserResult? CurrentUser
    {
        get
        {
            lock (_sync)
            {
                return _currentUser;
            }
        }
    }

    public void SignIn(UserResult user)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (!user.IsActive)
        {
            throw new InvalidOperationException("Inactive users cannot start a session.");
        }

        lock (_sync)
        {
            _currentUser = user;
        }
    }

    public void SignOut()
    {
        lock (_sync)
        {
            _currentUser = null;
        }
    }
}
