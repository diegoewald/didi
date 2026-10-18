namespace LocalErp.Application.Abstractions.Persistence;

public enum ProtectedRoleMutationResult
{
    Unchanged = 0,
    Changed = 1,
    WouldRemoveLastPermissionHolder = 2,
}
