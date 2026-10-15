using LocalErp.Application.Security;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;

namespace LocalErp.Infrastructure.Security;

public sealed class MicrosoftPasswordHashService : IPasswordHashService
{
    public const int MinimumIterationCount = 220_000;

    private static readonly PasswordHashSubject Subject = new();
    private readonly PasswordHasher<PasswordHashSubject> _hasher;

    public MicrosoftPasswordHashService()
        : this(MinimumIterationCount)
    {
    }

    public MicrosoftPasswordHashService(int iterationCount)
    {
        if (iterationCount < MinimumIterationCount)
        {
            throw new ArgumentOutOfRangeException(
                nameof(iterationCount),
                iterationCount,
                $"Password hashing requires at least {MinimumIterationCount} iterations.");
        }

        _hasher = new PasswordHasher<PasswordHashSubject>(
            Options.Create(new PasswordHasherOptions
            {
                CompatibilityMode = PasswordHasherCompatibilityMode.IdentityV3,
                IterationCount = iterationCount,
            }));
    }

    public string Hash(string password)
    {
        ArgumentNullException.ThrowIfNull(password);
        return _hasher.HashPassword(Subject, password);
    }

    public PasswordVerificationStatus Verify(string passwordHash, string providedPassword)
    {
        ArgumentNullException.ThrowIfNull(passwordHash);
        ArgumentNullException.ThrowIfNull(providedPassword);

        try
        {
            return _hasher.VerifyHashedPassword(Subject, passwordHash, providedPassword) switch
            {
                PasswordVerificationResult.Success => PasswordVerificationStatus.Success,
                PasswordVerificationResult.SuccessRehashNeeded => PasswordVerificationStatus.SuccessRehashNeeded,
                _ => PasswordVerificationStatus.Failed,
            };
        }
        catch (FormatException)
        {
            return PasswordVerificationStatus.Failed;
        }
    }

    private sealed class PasswordHashSubject
    {
    }
}
