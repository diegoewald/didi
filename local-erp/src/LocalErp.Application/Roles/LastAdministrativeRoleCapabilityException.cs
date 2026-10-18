namespace LocalErp.Application.Roles;

public sealed class LastAdministrativeRoleCapabilityException : InvalidOperationException
{
    public LastAdministrativeRoleCapabilityException()
        : base("The last effective role administration capability cannot be removed.")
    {
    }
}
