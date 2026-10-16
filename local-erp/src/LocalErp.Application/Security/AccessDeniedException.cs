namespace LocalErp.Application.Security;

public sealed class AccessDeniedException : UnauthorizedAccessException
{
    public AccessDeniedException()
    {
    }

    public AccessDeniedException(string? message)
        : base(message)
    {
    }

    public AccessDeniedException(string? message, Exception? innerException)
        : base(message, innerException)
    {
    }

    private AccessDeniedException(string message, string permissionCode)
        : base(message)
    {
        PermissionCode = permissionCode;
    }

    public string? PermissionCode { get; }

    public static AccessDeniedException ForPermission(string permissionCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(permissionCode);
        return new AccessDeniedException(
            $"Access denied for permission '{permissionCode}'.",
            permissionCode);
    }
}
