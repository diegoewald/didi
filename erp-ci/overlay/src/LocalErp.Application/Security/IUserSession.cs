using LocalErp.Application.Users;

namespace LocalErp.Application.Security;

public interface IUserSession
{
    bool IsAuthenticated { get; }

    UserResult? CurrentUser { get; }

    void SignIn(UserResult user);

    void SignOut();
}
