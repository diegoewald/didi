namespace LocalErp.Application.Security;

public static class AuthenticationThrottlePolicy
{
    public const int LockoutThreshold = 5;

    public static TimeSpan GetBlockDurationAfterFailure(int failedSignInCount)
    {
        if (failedSignInCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(failedSignInCount));
        }

        return failedSignInCount switch
        {
            1 or 2 => TimeSpan.Zero,
            3 => TimeSpan.FromSeconds(2),
            4 => TimeSpan.FromSeconds(10),
            _ => TimeSpan.FromMinutes(15),
        };
    }
}
