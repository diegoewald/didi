namespace LocalErp.Domain.Security;

public sealed class UserRole
{
    private UserRole()
    {
    }

    public UserRole(Guid userId, Guid roleId)
    {
        if (userId == Guid.Empty)
        {
            throw new ArgumentException("User identifier cannot be empty.", nameof(userId));
        }

        if (roleId == Guid.Empty)
        {
            throw new ArgumentException("Role identifier cannot be empty.", nameof(roleId));
        }

        UserId = userId;
        RoleId = roleId;
    }

    public Guid UserId { get; private set; }

    public Guid RoleId { get; private set; }
}
