namespace LocalErp.Application.Security;

public interface IPasswordHashService
{
    string Hash(string password);

    PasswordVerificationStatus Verify(string passwordHash, string providedPassword);
}
