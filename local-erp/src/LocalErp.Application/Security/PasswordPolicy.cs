namespace LocalErp.Application.Security;

public static class PasswordPolicy
{
    public const int MinimumLength = 15;
    public const int MaximumLength = 128;

    public static void Validate(string password)
    {
        ArgumentNullException.ThrowIfNull(password);

        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ArgumentException("Password cannot be empty or whitespace.", nameof(password));
        }

        if (password.Length < MinimumLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(password),
                password,
                $"Password must contain at least {MinimumLength} characters.");
        }

        if (password.Length > MaximumLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(password),
                password,
                $"Password cannot exceed {MaximumLength} characters.");
        }
    }
}
