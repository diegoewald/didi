using System.Text;
using LocalErp.Domain.Common;

namespace LocalErp.Domain.Users;

public sealed class User : Entity
{
    public const int UsernameMaxLength = 100;
    public const int PasswordHashMaxLength = 1024;

    private User()
    {
        Username = string.Empty;
        NormalizedUsername = string.Empty;
        PasswordHash = string.Empty;
    }

    public User(string username, string passwordHash)
    {
        SetUsername(username);
        SetPasswordHash(passwordHash);
        IsActive = true;
    }

    public string Username { get; private set; }

    public string NormalizedUsername { get; private set; }

    public string PasswordHash { get; private set; }

    public bool IsActive { get; private set; }

    public void Rename(string username)
    {
        SetUsername(username);
    }

    public void ChangePasswordHash(string passwordHash)
    {
        SetPasswordHash(passwordHash);
    }

    public void Activate()
    {
        IsActive = true;
    }

    public void Deactivate()
    {
        IsActive = false;
    }

    public static string NormalizeUsername(string username)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(username);

        var normalized = username.Trim().Normalize(NormalizationForm.FormKC);
        if (normalized.Length > UsernameMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(username),
                username,
                $"Username cannot exceed {UsernameMaxLength} characters.");
        }

        return normalized.ToUpperInvariant();
    }

    private void SetUsername(string username)
    {
        var normalized = NormalizeUsername(username);
        Username = username.Trim().Normalize(NormalizationForm.FormKC);
        NormalizedUsername = normalized;
    }

    private void SetPasswordHash(string passwordHash)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(passwordHash);

        var normalized = passwordHash.Trim();
        if (normalized.Length > PasswordHashMaxLength)
        {
            throw new ArgumentOutOfRangeException(
                nameof(passwordHash),
                passwordHash,
                $"Password hash cannot exceed {PasswordHashMaxLength} characters.");
        }

        PasswordHash = normalized;
    }
}
