namespace PrivacyComply.Application.Abstractions.Identity;

public interface IUserAccountAuthenticationCommands
{
    Task RecordSuccessfulSignInAsync(
        Guid userAccountId,
        DateTime successfulSignInDateTime,
        CancellationToken cancellationToken = default);

    Task RecordFailedSignInAsync(
        Guid userAccountId,
        int failedSignInCount,
        DateTime failedSignInDateTime,
        DateTime? lockoutEndDateTime,
        CancellationToken cancellationToken = default);

    Task ClearExpiredLockoutAsync(
        Guid userAccountId,
        CancellationToken cancellationToken = default);
}