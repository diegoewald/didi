using LocalErp.Application.Roles;
using LocalErp.Application.Users;

namespace LocalErp.Application.Security;

public sealed record AdministratorBootstrapResult(
    UserResult Administrator,
    RoleResult AdministratorRole);
