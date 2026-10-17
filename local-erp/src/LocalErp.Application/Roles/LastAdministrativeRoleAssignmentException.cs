namespace LocalErp.Application.Roles;

public sealed class LastAdministrativeRoleAssignmentException : InvalidOperationException
{
    public LastAdministrativeRoleAssignmentException()
        : base("The last effective role administration capability cannot be removed.")
    {
    }
}
