namespace LocalErp.Application.Security;

public interface IAuthenticationTimingProtector
{
    void PerformDummyVerification(string providedPassword);
}
