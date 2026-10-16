namespace LocalErp.Application.Abstractions.Persistence;

public interface IAuthenticationAttemptStore
{
    Task RecordFailedAttemptAsync(
        Guid userId,
        DateTime utcNow,
        CancellationToken cancellationToken = default);

    Task<bool> CompleteSuccessfulAttemptAsync(
        Guid userId,
        DateTime utcNow,
        string? upgradedPasswordHash,
        CancellationToken cancellationToken = default);
}
