namespace LocalErp.Application.Abstractions.Persistence;

public enum RoleAssignmentRemovalResult
{
    NotAssigned = 0,
    Removed = 1,
    WouldRemoveLastPermissionHolder = 2,
}
